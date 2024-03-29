﻿using UnityEngine;
using System.Collections.Generic;

public class Reservoir : ComputeShaderCreator
{
    public int Resolution;
    public Vector2Int Size;
    public ComputeBuffer Buffer; // 3D array, z=1 is for duplication for correct interpolation
    private Paint[] BufferData;

    public float PixelSize { get { return 1 / (float) Resolution; } }

    public Reservoir(int resolution, int width, int height, ShaderRegionFactory shaderRegionFactory, ComputeShaderEngine computeShaderEngine)
        : base(shaderRegionFactory, computeShaderEngine)
    {
        Resolution = resolution;
        Size = new Vector2Int(width, height);

        Buffer = new ComputeBuffer(width * height * 2, Paint.SizeInBytes);
        // initialize buffer to empty values (Intel does this for you, nvidia doesn't)
        BufferData = new Paint[width * height * 2];
        Buffer.SetData(BufferData);
    }

    public void Fill(ReservoirFiller filler)
    {
        filler.Fill(BufferData, Size);
        Buffer.SetData(BufferData);
    }

    public ShaderRegion GetShaderRegion()
    {
        return ShaderRegionFactory.Create(
            new Vector2Int(0, Size.y - 1),
            new Vector2Int(Size.x - 1, Size.y - 1),
            new Vector2Int(0, 0),
            new Vector2Int(Size.x - 1, 0)
        );
    }

    public void Duplicate(
        int discardVolumeThreshold,
        int smoothingKernelSize,
        bool debugEnabled = false)
    {
        ShaderRegion duplicateSR = GetShaderRegion();

        List<CSAttribute> attributes = new List<CSAttribute>()
        {
            new CSComputeBuffer("Reservoir", Buffer),
            new CSInt("DiscardVolumeThreshhold", discardVolumeThreshold),
            new CSInt("SmoothingKernelSize", smoothingKernelSize)
        };

        ComputeShaderTask cst = new ComputeShaderTask(
            "ReservoirDuplicationShader",
            duplicateSR,
            attributes,
            null,
            new List<ComputeBuffer>(),
            debugEnabled
        );

        ComputeShaderEngine.EnqueueOrRun(cst);
    }

    public void PrintVolumes(int z)
    {
        Buffer.GetData(BufferData);
        LogUtil.LogVolumes(BufferData, GetShaderRegion().CalculationSize.y, GetShaderRegion().CalculationSize.x, z, "z=" + z);

        //int sum = 0;
        //for (int i = 0; i < BufferData.GetLength(0) / 2; i++)
        //{
        //    sum += (int)BufferData[i].Volume;
        //}
        //Debug.Log("Sum is " + sum);
    }

    public void Dispose()
    {
        Buffer.Dispose();
    }
}
