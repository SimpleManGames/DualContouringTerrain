using UnityEngine;
using Unity.Entities;

public struct MeshData : IComponentData
{
    //public Mesh Mesh;
}

public class ApplyMeshData : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new MeshData());
    }
}