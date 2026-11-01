using System.Collections.Generic;
using System.Diagnostics;
using DefenseGame.Data;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace DefenseGame.InGame.Swarm
{
    /// <summary>
    /// 수백~수천 마리 몬스터를 Job System + Burst로 움직이는 부하 테스트 시제품입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 몬스터 상태는 몬스터별 객체가 아니라 NativeArray 묶음으로 들고, 화면 표시만 풀링한 SpriteRenderer 오브젝트가 맡습니다.
    /// Update에서 잡을 예약하고 LateUpdate에서 끝나기를 기다리므로, 그 사이 메인 스레드는 다른 일을 할 수 있습니다.
    /// </para>
    /// <para>
    /// 잡 순서: 칸 번호 → 격자 정렬 → (이동 방향 ∥ 포탑 사격) → 위치 적분·리스폰 → Transform 쓰기.
    /// 처치된 몬스터는 화면 위에서 다시 나와 마리 수가 유지됩니다(부하 측정용).
    /// </para>
    /// <para>WebGL은 잡이 메인 스레드에서 돌아 병렬 효과가 없으므로, 같은 마리 수를 기대하면 안 됩니다.</para>
    /// </remarks>
    public sealed class MonsterSwarm : MonoBehaviour
    {
        [Header("몬스터")]
        [SerializeField] private Sprite _monsterSprite;
        [SerializeField] private int _initialCount = 500;
        [SerializeField] private int _maxCount = 3000;

        [Header("전장 (월드 좌표)")]
        [SerializeField] private float _halfWidth = 5.2f;
        [SerializeField] private float _wallY = -6f;
        [SerializeField] private float _spawnY = 10.5f;
        [SerializeField] private float _spawnHeight = 6f;
        [SerializeField] private float _visibleTopY = 9.6f;
        [SerializeField] private float _cellSize = 0.6f;

        [Header("밀어내기")]
        [SerializeField] private float _separationStrength = 6f;
        [SerializeField] private float _maxPushSpeed = 3f;
        [SerializeField] private float _wanderSpeed = 0.15f;

        [Header("포탑")]
        [SerializeField] private Vector2[] _turretPositions = { new(-4f, -7.2f), new(-2f, -7.2f), new(0f, -7.2f), new(2f, -7.2f), new(4f, -7.2f) };
        [SerializeField] private float _turretRange = 8f;
        [SerializeField] private float _turretFireRate = 6f;
        [SerializeField] private float _turretDamage = 25f;

        private const int BossInterval = 250;

        private readonly List<int> _normalStats = new();
        private readonly List<SpriteRenderer> _renderers = new();
        private readonly Stack<SpriteRenderer> _pool = new();
        private readonly Stopwatch _waitWatch = new();

        private SwarmGrid _grid;
        private NativeArray<MonsterStats> _stats;
        private NativeArray<float2> _positions;
        private NativeArray<float2> _velocities;
        private NativeArray<float> _hp;
        private NativeArray<float> _maxHp;
        private NativeArray<float> _radii;
        private NativeArray<float> _speeds;
        private NativeArray<float> _phases;
        private NativeArray<float> _scales;
        private NativeArray<byte> _killed;
        private NativeArray<int> _cellIndices;
        private NativeArray<int> _cellStarts;
        private NativeArray<int> _sortedIndices;
        private NativeArray<float2> _turrets;
        private NativeArray<float> _turretCooldowns;
        private NativeArray<float2> _shotPoints;
        private TransformAccessArray _transforms;
        private SpriteRenderer[] _tracers;

        private JobHandle _handle;
        private bool _scheduled;
        private bool _running;
        private int _count;
        private int _targetCount;
        private int _bossStats = -1;

        public int Count => _count;

        public int MaxCount => _maxCount;

        public int Kills { get; private set; }

        /// <summary>LateUpdate에서 잡이 끝나기를 기다린 시간(밀리초)입니다. 메인 스레드가 잡 때문에 멈춘 시간입니다.</summary>
        public double JobWaitMilliseconds { get; private set; }

        /// <summary>
        /// 스테이지의 몬스터로 시작합니다. 잡 안에서는 class를 쓸 수 없으므로, 여기서 능력치를 구조체 배열로 옮깁니다.
        /// </summary>
        public void Begin(StageData stage)
        {
            AllocateBuffers(stage);
            CreateTurretVisuals();
            SetTargetCount(_initialCount);
            _running = true;
        }

        /// <summary>마리 수를 바꿉니다. 다음 Update에서 잡이 돌지 않을 때 적용합니다.</summary>
        public void SetTargetCount(int count) => _targetCount = Mathf.Clamp(count, 0, _maxCount);

        private void AllocateBuffers(StageData stage)
        {
            var monsters = stage.MonsterRows;
            _stats = new NativeArray<MonsterStats>(monsters.Count, Allocator.Persistent);
            for (var i = 0; i < monsters.Count; i++)
            {
                _stats[i] = MonsterStats.From(monsters[i], stage.HpScale);
                if (_stats[i].IsBoss)
                    _bossStats = i;
                else
                    _normalStats.Add(i);
            }

            _positions = new NativeArray<float2>(_maxCount, Allocator.Persistent);
            _velocities = new NativeArray<float2>(_maxCount, Allocator.Persistent);
            _hp = new NativeArray<float>(_maxCount, Allocator.Persistent);
            _maxHp = new NativeArray<float>(_maxCount, Allocator.Persistent);
            _radii = new NativeArray<float>(_maxCount, Allocator.Persistent);
            _speeds = new NativeArray<float>(_maxCount, Allocator.Persistent);
            _phases = new NativeArray<float>(_maxCount, Allocator.Persistent);
            _scales = new NativeArray<float>(_maxCount, Allocator.Persistent);
            _killed = new NativeArray<byte>(_maxCount, Allocator.Persistent);
            _cellIndices = new NativeArray<int>(_maxCount, Allocator.Persistent);
            _sortedIndices = new NativeArray<int>(_maxCount, Allocator.Persistent);
            _transforms = new TransformAccessArray(_maxCount);

            var min = new float2(-_halfWidth - 1f, _wallY - 2f);
            var max = new float2(_halfWidth + 1f, _spawnY + _spawnHeight + 1f);
            _grid = new SwarmGrid(min, max, _cellSize);
            _cellStarts = new NativeArray<int>(_grid.CellCount + 1, Allocator.Persistent);

            _turrets = new NativeArray<float2>(_turretPositions.Length, Allocator.Persistent);
            for (var i = 0; i < _turretPositions.Length; i++)
                _turrets[i] = _turretPositions[i];
            _turretCooldowns = new NativeArray<float>(_turretPositions.Length, Allocator.Persistent);
            _shotPoints = new NativeArray<float2>(_turretPositions.Length, Allocator.Persistent);
        }

        private void Update()
        {
            if (!_running)
                return;

            ApplyTargetCount();
            if (_count == 0)
                return;

            var deltaTime = Time.deltaTime;
            var cells = new CellIndexJob
            {
                Positions = _positions,
                Grid = _grid,
                CellIndices = _cellIndices,
            }.ScheduleParallel(_count, 128, default);

            var grid = new BuildGridJob
            {
                Count = _count,
                CellIndices = _cellIndices,
                CellStarts = _cellStarts,
                SortedIndices = _sortedIndices,
            }.Schedule(cells);

            var steer = new SteerJob
            {
                Positions = _positions,
                Radii = _radii,
                Speeds = _speeds,
                Phases = _phases,
                CellStarts = _cellStarts,
                SortedIndices = _sortedIndices,
                Grid = _grid,
                WallY = _wallY,
                SeparationStrength = _separationStrength,
                MaxPushSpeed = _maxPushSpeed,
                WanderSpeed = _wanderSpeed,
                Velocities = _velocities,
            }.ScheduleParallel(_count, 32, grid);

            var turrets = new TurretJob
            {
                Positions = _positions,
                CellStarts = _cellStarts,
                SortedIndices = _sortedIndices,
                TurretPositions = _turrets,
                Grid = _grid,
                DeltaTime = deltaTime,
                Range = _turretRange,
                FireInterval = 1f / _turretFireRate,
                Damage = _turretDamage,
                VisibleTopY = _visibleTopY,
                Hp = _hp,
                Cooldowns = _turretCooldowns,
                ShotPoints = _shotPoints,
            }.Schedule(grid);

            var integrate = new IntegrateJob
            {
                Velocities = _velocities,
                Radii = _radii,
                MaxHp = _maxHp,
                Positions = _positions,
                Hp = _hp,
                Phases = _phases,
                Killed = _killed,
                DeltaTime = deltaTime,
                WallY = _wallY,
                HalfWidth = _halfWidth,
                SpawnY = _spawnY,
                SpawnHeight = _spawnHeight,
                Seed = (uint)Time.frameCount * 7919u,
            }.ScheduleParallel(_count, 128, JobHandle.CombineDependencies(steer, turrets));

            _handle = new WriteTransformsJob
            {
                Positions = _positions,
                Phases = _phases,
                Scales = _scales,
            }.Schedule(_transforms, integrate);

            _scheduled = true;
            JobHandle.ScheduleBatchedJobs();
        }

        private void LateUpdate()
        {
            if (!_scheduled)
                return;

            _waitWatch.Restart();
            _handle.Complete();
            _waitWatch.Stop();
            JobWaitMilliseconds = _waitWatch.Elapsed.TotalMilliseconds;
            _scheduled = false;

            for (var i = 0; i < _count; i++)
                Kills += _killed[i];
            ShowTracers();
        }

        private void OnDestroy()
        {
            _handle.Complete();
            if (!_stats.IsCreated)
                return;

            _stats.Dispose();
            _positions.Dispose();
            _velocities.Dispose();
            _hp.Dispose();
            _maxHp.Dispose();
            _radii.Dispose();
            _speeds.Dispose();
            _phases.Dispose();
            _scales.Dispose();
            _killed.Dispose();
            _cellIndices.Dispose();
            _cellStarts.Dispose();
            _sortedIndices.Dispose();
            _turrets.Dispose();
            _turretCooldowns.Dispose();
            _shotPoints.Dispose();
            _transforms.Dispose();
        }

        #region 마리 수 조절

        // 슬롯 i의 데이터와 _transforms[i], _renderers[i]가 항상 같은 몬스터를 가리키도록, 늘릴 때는 뒤에 붙이고 줄일 때는 뒤에서 뺍니다.
        private void ApplyTargetCount()
        {
            while (_count < _targetCount)
                AddMonster();
            while (_count > _targetCount)
                RemoveLastMonster();
        }

        private void AddMonster()
        {
            var index = _count;
            var statsIndex = PickStats(index);
            var stats = _stats[statsIndex];

            _radii[index] = stats.Radius;
            _speeds[index] = stats.Speed;
            _maxHp[index] = stats.MaxHp;
            _hp[index] = stats.MaxHp;
            _phases[index] = index * 1.7f;
            _scales[index] = stats.Radius * 2f / _monsterSprite.bounds.size.x;
            _killed[index] = 0;

            var random = Unity.Mathematics.Random.CreateFromIndex((uint)(index + Time.frameCount * 31));
            _positions[index] = new float2(random.NextFloat(-_halfWidth + stats.Radius, _halfWidth - stats.Radius), _spawnY + random.NextFloat(0f, _spawnHeight));

            var renderer = _pool.Count > 0 ? _pool.Pop() : CreateRenderer();
            renderer.color = stats.Color;
            renderer.sortingOrder = stats.SortingOrder;
            renderer.transform.position = new Vector3(_positions[index].x, _positions[index].y, 0f);
            renderer.gameObject.SetActive(true);
            _renderers.Add(renderer);
            _transforms.Add(renderer.transform);
            _count++;
        }

        private void RemoveLastMonster()
        {
            var index = _count - 1;
            var renderer = _renderers[index];
            _renderers.RemoveAt(index);
            _transforms.RemoveAtSwapBack(index);
            renderer.gameObject.SetActive(false);
            _pool.Push(renderer);
            _count--;
        }

        // 보스는 BossInterval마리 중 한 마리만 나오게 하고, 나머지는 보스가 아닌 몬스터를 돌아가며 씁니다.
        private int PickStats(int index)
        {
            var hasBoss = _bossStats >= 0;
            if (hasBoss && (_normalStats.Count == 0 || index % BossInterval == BossInterval - 1))
                return _bossStats;
            return _normalStats.Count > 0 ? _normalStats[index % _normalStats.Count] : 0;
        }

        // 잡에서 Transform을 여러 스레드로 나눠 쓰려면 최상위 오브젝트여야 해서 부모를 두지 않습니다.
        private SpriteRenderer CreateRenderer()
        {
            var go = new GameObject("Monster");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _monsterSprite;
            return renderer;
        }

        #endregion

        #region 포탑 표시

        private void CreateTurretVisuals()
        {
            _tracers = new SpriteRenderer[_turretPositions.Length];
            for (var i = 0; i < _turretPositions.Length; i++)
            {
                var turret = new GameObject("Turret" + (i + 1)).AddComponent<SpriteRenderer>();
                turret.sprite = _monsterSprite;
                turret.color = new Color32(0x5F, 0xE1, 0xFF, 0xFF);
                turret.sortingOrder = 20;
                turret.transform.SetParent(transform, false);
                turret.transform.position = _turretPositions[i];
                turret.transform.localScale = Vector3.one * (0.7f / _monsterSprite.bounds.size.x);

                var tracer = new GameObject("Tracer" + (i + 1)).AddComponent<SpriteRenderer>();
                tracer.sprite = _monsterSprite;
                tracer.color = new Color(1f, 0.9f, 0.5f, 0.9f);
                tracer.sortingOrder = 30;
                tracer.transform.SetParent(transform, false);
                tracer.gameObject.SetActive(false);
                _tracers[i] = tracer;
            }
        }

        private void ShowTracers()
        {
            var spriteSize = _monsterSprite.bounds.size.x;
            for (var i = 0; i < _tracers.Length; i++)
            {
                var point = _shotPoints[i];
                var tracer = _tracers[i];
                if (math.any(math.isnan(point)))
                {
                    tracer.gameObject.SetActive(false);
                    continue;
                }

                var from = _turretPositions[i];
                var to = new Vector2(point.x, point.y);
                var delta = to - from;
                tracer.transform.position = (from + to) * 0.5f;
                tracer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                tracer.transform.localScale = new Vector3(delta.magnitude / spriteSize, 0.06f / spriteSize, 1f);
                tracer.gameObject.SetActive(true);
            }
        }

        #endregion
    }
}
