using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace DefenseGame.InGame.Swarm
{
    /// <summary>몬스터마다 들어 있는 칸 번호를 구합니다.</summary>
    [BurstCompile]
    public struct CellIndexJob : IJobFor
    {
        [ReadOnly] public NativeArray<float2> Positions;
        public SwarmGrid Grid;
        [WriteOnly] public NativeArray<int> CellIndices;

        public void Execute(int index) => CellIndices[index] = Grid.IndexOf(Positions[index]);
    }

    /// <summary>
    /// 칸 번호로 카운팅 정렬해서 칸별 몬스터 목록을 만듭니다. 몬스터 수천 마리까지는 한 스레드로도 충분히 빠릅니다.
    /// </summary>
    [BurstCompile]
    public struct BuildGridJob : IJob
    {
        public int Count;
        [ReadOnly] public NativeArray<int> CellIndices;

        /// <summary>길이는 칸 수 + 1입니다.</summary>
        public NativeArray<int> CellStarts;

        [WriteOnly] public NativeArray<int> SortedIndices;

        public void Execute()
        {
            for (var c = 0; c < CellStarts.Length; c++)
                CellStarts[c] = 0;
            for (var i = 0; i < Count; i++)
                CellStarts[CellIndices[i] + 1]++;
            for (var c = 1; c < CellStarts.Length; c++)
                CellStarts[c] += CellStarts[c - 1];

            var cursor = new NativeArray<int>(CellStarts, Allocator.Temp);
            for (var i = 0; i < Count; i++)
            {
                var cell = CellIndices[i];
                SortedIndices[cursor[cell]] = i;
                cursor[cell]++;
            }
        }
    }

    /// <summary>
    /// 이동 방향을 정합니다. 기지 쪽(아래)으로 걸어가고, 겹친 이웃은 서로 밀어냅니다. 방벽에 닿으면 멈춰 쌓입니다.
    /// </summary>
    /// <remarks>위치는 읽기만 하고 속도만 씁니다. 그래서 몬스터끼리 병렬로 계산해도 서로의 결과를 덮어쓰지 않습니다.</remarks>
    [BurstCompile]
    public struct SteerJob : IJobFor
    {
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<float> Radii;
        [ReadOnly] public NativeArray<float> Speeds;
        [ReadOnly] public NativeArray<float> Phases;
        [ReadOnly] public NativeArray<int> CellStarts;
        [ReadOnly] public NativeArray<int> SortedIndices;
        public SwarmGrid Grid;
        public float WallY;
        public float SeparationStrength;
        public float MaxPushSpeed;
        public float WanderSpeed;

        [WriteOnly] public NativeArray<float2> Velocities;

        public void Execute(int index)
        {
            var position = Positions[index];
            var radius = Radii[index];
            var push = float2.zero;

            var center = Grid.CellOf(position);
            var min = math.max(center - 1, 0);
            var max = math.min(center + 1, Grid.Size - 1);
            for (var y = min.y; y <= max.y; y++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var cell = Grid.IndexOf(new int2(x, y));
                    for (var k = CellStarts[cell]; k < CellStarts[cell + 1]; k++)
                    {
                        var other = SortedIndices[k];
                        if (other == index)
                            continue;

                        var offset = position - Positions[other];
                        var distanceSq = math.lengthsq(offset);
                        var minDistance = radius + Radii[other];
                        if (distanceSq >= minDistance * minDistance)
                            continue;

                        if (distanceSq < 1e-8f)
                        {
                            // 완전히 겹치면 방향이 없으므로 번호로 좌우를 정합니다.
                            push.x += index < other ? -minDistance : minDistance;
                            continue;
                        }

                        var distance = math.sqrt(distanceSq);
                        push += offset / distance * (minDistance - distance);
                    }
                }
            }

            var pushVelocity = push * SeparationStrength;
            var pushSpeed = math.length(pushVelocity);
            if (pushSpeed > MaxPushSpeed)
                pushVelocity *= MaxPushSpeed / pushSpeed;

            var atWall = position.y - radius <= WallY;
            var forward = atWall ? 0f : -Speeds[index];
            var wander = math.sin(Phases[index] * 0.37f) * WanderSpeed;
            Velocities[index] = new float2(wander, forward) + pushVelocity;
        }
    }

    /// <summary>
    /// 포탑마다 사거리 안에서 가장 가까운 몬스터를 격자로 찾아 피해를 줍니다.
    /// </summary>
    /// <remarks>포탑 여러 개가 같은 몬스터를 때릴 수 있어서, 체력 쓰기가 겹치지 않도록 한 스레드에서 돕니다. 포탑은 수가 적어 충분히 빠릅니다.</remarks>
    [BurstCompile]
    public struct TurretJob : IJob
    {
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<int> CellStarts;
        [ReadOnly] public NativeArray<int> SortedIndices;
        [ReadOnly] public NativeArray<float2> TurretPositions;
        public SwarmGrid Grid;
        public float DeltaTime;
        public float Range;
        public float FireInterval;
        public float Damage;
        public float VisibleTopY;

        public NativeArray<float> Hp;
        public NativeArray<float> Cooldowns;

        /// <summary>이번 프레임에 쏜 포탑은 맞힌 위치, 안 쏜 포탑은 NaN입니다. 탄 궤적 표시에 씁니다.</summary>
        [WriteOnly] public NativeArray<float2> ShotPoints;

        public void Execute()
        {
            for (var t = 0; t < TurretPositions.Length; t++)
            {
                ShotPoints[t] = new float2(float.NaN);
                var cooldown = Cooldowns[t] - DeltaTime;
                if (cooldown > 0f)
                {
                    Cooldowns[t] = cooldown;
                    continue;
                }

                var target = FindNearest(TurretPositions[t]);
                if (target < 0)
                {
                    Cooldowns[t] = 0f;
                    continue;
                }

                Hp[target] -= Damage;
                ShotPoints[t] = Positions[target];
                Cooldowns[t] = math.max(cooldown + FireInterval, 0f);
            }
        }

        private int FindNearest(float2 origin)
        {
            var best = -1;
            var bestDistanceSq = Range * Range;
            var min = Grid.CellOf(origin - Range);
            var max = Grid.CellOf(origin + Range);
            for (var y = min.y; y <= max.y; y++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var cell = Grid.IndexOf(new int2(x, y));
                    for (var k = CellStarts[cell]; k < CellStarts[cell + 1]; k++)
                    {
                        var index = SortedIndices[k];
                        var position = Positions[index];
                        if (Hp[index] <= 0f || position.y > VisibleTopY)
                            continue;

                        var distanceSq = math.distancesq(origin, position);
                        if (distanceSq < bestDistanceSq)
                        {
                            bestDistanceSq = distanceSq;
                            best = index;
                        }
                    }
                }
            }
            return best;
        }
    }

    /// <summary>
    /// 속도로 위치를 옮깁니다. 체력이 0 이하인 몬스터는 처치로 기록하고 화면 위에서 다시 나오게 합니다(부하 테스트용으로 마리 수 유지).
    /// </summary>
    [BurstCompile]
    public struct IntegrateJob : IJobFor
    {
        [ReadOnly] public NativeArray<float2> Velocities;
        [ReadOnly] public NativeArray<float> Radii;
        [ReadOnly] public NativeArray<float> MaxHp;
        public NativeArray<float2> Positions;
        public NativeArray<float> Hp;
        public NativeArray<float> Phases;

        /// <summary>이번 프레임에 처치되었으면 1입니다.</summary>
        [WriteOnly] public NativeArray<byte> Killed;

        public float DeltaTime;
        public float WallY;
        public float HalfWidth;
        public float SpawnY;
        public float SpawnHeight;
        public uint Seed;

        public void Execute(int index)
        {
            var radius = Radii[index];
            if (Hp[index] <= 0f)
            {
                var random = Unity.Mathematics.Random.CreateFromIndex(Seed + (uint)index);
                Positions[index] = new float2(random.NextFloat(-HalfWidth + radius, HalfWidth - radius), SpawnY + random.NextFloat(0f, SpawnHeight));
                Hp[index] = MaxHp[index];
                Killed[index] = 1;
                return;
            }

            var position = Positions[index] + Velocities[index] * DeltaTime;
            position.x = math.clamp(position.x, -HalfWidth + radius, HalfWidth - radius);
            position.y = math.max(position.y, WallY + radius * 0.5f);
            Positions[index] = position;
            Phases[index] += DeltaTime * 9f;
            Killed[index] = 0;
        }
    }

    /// <summary>
    /// 계산한 위치를 Transform에 씁니다. 꿈틀거리는 느낌을 내려고 크기를 살짝 늘였다 줄입니다.
    /// </summary>
    /// <remarks>Transform은 최상위(부모 없는) 오브젝트여야 여러 스레드로 나뉩니다. 한 부모 아래에 모으면 한 스레드에서 돕니다.</remarks>
    [BurstCompile]
    public struct WriteTransformsJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<float> Phases;
        [ReadOnly] public NativeArray<float> Scales;

        public void Execute(int index, TransformAccess transform)
        {
            var position = Positions[index];
            var squash = 0.07f * math.sin(Phases[index]);
            var scale = Scales[index];
            transform.localPosition = new Vector3(position.x, position.y, 0f);
            transform.localScale = new Vector3(scale * (1f + squash), scale * (1f - squash), 1f);
        }
    }
}
