﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class OilPaintCanvas
{
    public WorldSpaceCanvas WorldSpaceCanvas { get; private set; }
    public ComputeBuffer Reservoir { get; private set; }
    public RenderTexture Texture { get; private set; }
    public RenderTexture NormalMap { get; private set; }

    public OilPaintCanvas(int textureResolution)
    {
        Renderer renderer = GameObject.Find("Canvas").GetComponent<Renderer>();
        float width = GameObject.Find("Canvas").GetComponent<Transform>().localScale.x * 10; // convert scale attribute to world space
        float height = GameObject.Find("Canvas").GetComponent<Transform>().localScale.y * 10; // convert scale attribute to world space
        Vector3 position = GameObject.Find("Canvas").GetComponent<Transform>().position;

        WorldSpaceCanvas = new WorldSpaceCanvas(height, width, textureResolution, position);

        Reservoir = new ComputeBuffer(WorldSpaceCanvas.TextureSize.x * WorldSpaceCanvas.TextureSize.y, sizeof(float) * 4 + sizeof(int));

        Texture = new RenderTexture(WorldSpaceCanvas.TextureSize.x, WorldSpaceCanvas.TextureSize.y, 1);
        Texture.filterMode = FilterMode.Point;
        Texture.enableRandomWrite = true;
        Texture.Create();
        renderer.material.SetTexture("_MainTex", Texture);

        NormalMap = new RenderTexture(WorldSpaceCanvas.TextureSize.x, WorldSpaceCanvas.TextureSize.y, 1);
        NormalMap.filterMode = FilterMode.Point;
        NormalMap.enableRandomWrite = true;
        NormalMap.Create();
        renderer.material.EnableKeyword("_NORMALMAP");
        renderer.material.SetTexture("_BumpMap", NormalMap);

        InitializeTexture(Texture, Vector4.one);
        InitializeTexture(NormalMap, (new Vector4(0, 0, 1, 0) + Vector4.one) / 2);
    }

    private void InitializeTexture(RenderTexture texture, Vector4 value)
    {
        IntelGPUShaderRegion sr = new IntelGPUShaderRegion(
            new Vector2Int(texture.height, 0),
            new Vector2Int(texture.height, texture.width),
            new Vector2Int(0, 0),
            new Vector2Int(texture.width, 0)
        );

        ComputeShaderTask cst = new ComputeShaderTask(
            "SetTextureShader",
            ComputeShaderUtil.LoadComputeShader("SetTextureShader"),
            new List<CSAttribute>() {
                new CSInts2("CalculationSize", new Vector2Int(texture.width, texture.height)),
                new CSFloats4("Value", value),
                new CSTexture("Target", texture)
            },
            sr.ThreadGroups,
            null,
            new List<ComputeBuffer>(),
            null
        );

        cst.Run();
    }

    public void Dispose()
    {
        Reservoir.Dispose();
    }
}