using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

// Tag for indicating if a mesh needs to be generated
public struct MeshGeneratorTag : IComponentData { }

// Buffer Array for Vertex Positions
public struct MeshGeneratorVertexData : IBufferElementData
{
    public float3 Vertex;
}

// Buffer Array for Triangles
public struct MeshGeneratorTriangleData : IBufferElementData
{
    public int Triangle;
}

public class MeshGenerator : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Add the buffers
        dstManager.AddBuffer<MeshGeneratorVertexData>(entity);
        dstManager.AddBuffer<MeshGeneratorTriangleData>(entity);

        // Prompt the system for building the mesh by adding the Tag
        dstManager.AddComponentData(entity, new MeshGeneratorTag());
        dstManager.AddSharedComponentData(entity, new RenderMesh());


    }
}

public class MeshGeneratorSystem : JobComponentSystem
{
    private Mesh mesh;

    protected override void OnCreate()
    {
        base.OnCreate();

        mesh = new Mesh();
    }

    [RequireComponentTag(typeof(MeshGeneratorVertexData), typeof(MeshGeneratorTriangleData))]
    private struct MeshGeneratorJob : IJobForEachWithEntity<MeshGeneratorTag>
    {
        // NativeDisableParallelForRestriction lets us modify these values even though Unity doesn't think it is Thread safe
        // Buffer data that is passed in when we create the job
        [NativeDisableParallelForRestriction] public BufferFromEntity<MeshGeneratorVertexData> vertexData;
        [NativeDisableParallelForRestriction] public BufferFromEntity<MeshGeneratorTriangleData> triangleData;

        // Location were we will store the data to pass back to main thread
        public NativeArray<Entity> thisEntity;

        public void Execute(Entity entity, int index, [ReadOnly] ref MeshGeneratorTag tag)
        {
            this.thisEntity[0] = entity;

            var vertexArray = vertexData[entity];
            vertexArray.Add(new MeshGeneratorVertexData { Vertex = new float3(0, 0, 0) });
            vertexArray.Add(new MeshGeneratorVertexData { Vertex = new float3(0, 0, 1) });
            vertexArray.Add(new MeshGeneratorVertexData { Vertex = new float3(1, 0, 0) });

            var triangleArray = triangleData[entity];
            triangleArray.Add(new MeshGeneratorTriangleData() { Triangle = 0 });
            triangleArray.Add(new MeshGeneratorTriangleData() { Triangle = 1 });
            triangleArray.Add(new MeshGeneratorTriangleData() { Triangle = 2 });
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Create a location to recieve data back from the thread
        NativeArray<Entity> entity = new NativeArray<Entity>(1, Allocator.TempJob);

        // Create the job
        // Passing in the Vertex and Triangle buffers from the entity we are working on
        // As well as the NativeArray we made to recieve the entity
        MeshGeneratorJob meshGenJob = new MeshGeneratorJob()
        {
            vertexData = GetBufferFromEntity<MeshGeneratorVertexData>(false),
            triangleData = GetBufferFromEntity<MeshGeneratorTriangleData>(false),
            thisEntity = entity
        };

        JobHandle handle = meshGenJob.Schedule(this, inputDeps);
        handle.Complete();

        if (entity[0] == Entity.Null)
        {
            entity.Dispose();
            return handle;
        }

        // Once job is done we have the entity that we worked on
        // So now that the job is over with we can remove the tag
        // So it won't update anymore till we re-add the tag to
        // Prompt a re-build
        EntityManager.RemoveComponent(entity[0], typeof(MeshGeneratorTag));

        var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(entity[0]);

        NativeArray<Vector3> nativeVertexArray = GetBufferFromEntity<MeshGeneratorVertexData>(true)[entity[0]].Reinterpret<Vector3>().ToNativeArray(Allocator.TempJob);
        NativeArray<int> nativeTriangleArray = GetBufferFromEntity<MeshGeneratorTriangleData>(true)[entity[0]].Reinterpret<int>().ToNativeArray(Allocator.TempJob);

        mesh.vertices = nativeVertexArray.ToArray();
        mesh.triangles = nativeTriangleArray.ToArray();

        mesh.RecalculateNormals();

        nativeVertexArray.Dispose();
        nativeTriangleArray.Dispose();

        EntityManager.SetSharedComponentData(entity[0], new RenderMesh
        {
            mesh = mesh
        });

        Debug.Log(EntityManager.GetSharedComponentData<RenderMesh>(entity[0]).mesh.vertexCount);

        entity.Dispose();

        return handle;
    }
}