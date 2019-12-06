using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

public struct TerrainSettings
{

}

public struct TerrainData : IComponentData
{

}

public class PopulateMeshData : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        
    }
}

public class PopulateMeshDataSystem : JobComponentSystem
{
    private TerrainSettings settings;

    protected override void OnCreate()
    {
        base.OnCreate();


    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return inputDeps;
    }
}