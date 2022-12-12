using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Threading;

public class OilPaintEngine : MonoBehaviour
{
    public bool BENCHMARK = false;
    private Camera Camera;

    public int TextureResolution { get; private set; } = 100; // texture space pixels per 1 world space
    private RenderTexture Texture;
    private RenderTexture NormalMap;

    private int CanvasColliderID;
    private Renderer CanvasRenderer;
    private float CanvasWidth; // world space
    private float CanvasHeight; // world space
    private Vector3 CanvasPosition; // world space
    private WorldSpaceCanvas WorldSpaceCanvas;
    private ComputeBuffer Canvas;

    //private IRakelPaintReservoir RakelPaintReservoir;
    //public Paint RakelPaint { get; private set; }

    private static Vector2 NO_VECTOR2 = new Vector2(float.NaN, float.NaN);
    private bool PreviousMousePositionInitialized = false;
    private Vector2 PreviousMousePosition = NO_VECTOR2;
    public float RakelRotation { get; private set; } = 0;
    public float RakelLength { get; private set; } = 8f; // world space
    public float RakelWidth { get; private set; } = 0.3f; // world space
    private IRakel Rakel;

    private RakelInterpolator RakelInterpolator;


    private Queue<ComputeShaderTask> ComputeShaderTasks = new Queue<ComputeShaderTask>();

    void Awake()
    {
        Camera = GameObject.Find("Main Camera").GetComponent<Camera>();

        CanvasColliderID = GameObject.Find("Canvas").GetComponent<MeshCollider>().GetInstanceID();
        CanvasRenderer = GameObject.Find("Canvas").GetComponent<Renderer>();
        CanvasWidth = GameObject.Find("Canvas").GetComponent<Transform>().localScale.x * 10; // convert scale attribute to world space
        CanvasHeight = GameObject.Find("Canvas").GetComponent<Transform>().localScale.y * 10; // convert scale attribute to world space
        CanvasPosition = GameObject.Find("Canvas").GetComponent<Transform>().position;

        CreateCanvasAndTools();
    }

    void Start()
    {
    }

    void CreateCanvasAndTools()
    {
        WorldSpaceCanvas = new WorldSpaceCanvas(CanvasHeight, CanvasWidth, TextureResolution, CanvasPosition);
        Canvas = new ComputeBuffer(WorldSpaceCanvas.TextureSize.x * WorldSpaceCanvas.TextureSize.y, sizeof(float) * 4 + sizeof(int));

        Texture = new RenderTexture(WorldSpaceCanvas.TextureSize.x, WorldSpaceCanvas.TextureSize.y, 1);
        Texture.filterMode = FilterMode.Point;
        Texture.enableRandomWrite = true;
        Texture.Create();
        CanvasRenderer.material.SetTexture("_MainTex", Texture);

        NormalMap = new RenderTexture(WorldSpaceCanvas.TextureSize.x, WorldSpaceCanvas.TextureSize.y, 1);
        NormalMap.filterMode = FilterMode.Point;
        NormalMap.enableRandomWrite = true;
        NormalMap.Create();
        CanvasRenderer.material.EnableKeyword("_NORMALMAP");
        CanvasRenderer.material.SetTexture("_BumpMap", NormalMap);

        CreateRakelDrawer();
    }

    void CreateRakelDrawer()
    {
        //RakelPaintReservoir = new RakelPaintReservoir(
        //    WorldSpaceLengthToTextureSpaceLength(RakelLength, TextureResolution),
        //    WorldSpaceLengthToTextureSpaceLength(RakelWidth, TextureResolution));
        if (Rakel != null)
            Rakel.Dispose();
                    
        Rakel = new Rakel(RakelLength, RakelWidth, TextureResolution, ComputeShaderTasks);
        int rakelPixelsLength = MathUtil.ToTextureSpaceLength(RakelLength, TextureResolution);
        int rakelPixelsWidth = MathUtil.ToTextureSpaceLength(RakelWidth, TextureResolution);
        Debug.Log("Rakel is " + rakelPixelsLength + "x" + rakelPixelsWidth + " = " + rakelPixelsLength * rakelPixelsWidth);

        RakelInterpolator = new RakelInterpolator(Rakel, WorldSpaceCanvas, Canvas, Texture, NormalMap);
    }

    void Update()
    {
        if (BENCHMARK){
            for (int i=0; i<50; i++){
                float x = Random.Range(-5f, 5f);
                float y = Random.Range(-3f, 3f);
                Rakel.Apply(new Vector3(x,y,0), 0, 0, WorldSpaceCanvas, Canvas, Texture, NormalMap);
            }
        } else {
            Vector2 currentMousePosition = Input.mousePosition;
            if (!PreviousMousePositionInitialized){
                PreviousMousePosition = currentMousePosition;
                PreviousMousePositionInitialized = true;
            } else {
                Vector2 direction = currentMousePosition - PreviousMousePosition;

                if (direction.magnitude > 8) {
                    float angle = MathUtil.Angle360(Vector2.right, direction);
                    RakelRotation = angle;

                    PreviousMousePosition = currentMousePosition;
                }
            }

            Vector3 worldSpaceHit = InputUtil.GetMouseHit(Camera, CanvasColliderID);
            if (!worldSpaceHit.Equals(Vector3.negativeInfinity))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    RakelInterpolator.NewStroke();
                }
                //RakelInterpolator.AddNode(worldSpaceHit, RakelRotation, 0, TextureResolution);
                RakelInterpolator.AddNode(worldSpaceHit, 0, 0, TextureResolution);
            }
        }

        processComputeShaderTasks();
    }

    private void processComputeShaderTasks()
    {
        int left = 100;
        while (ComputeShaderTasks.Count > 0 && left-- > 0)
        {
            ComputeShaderTask cst = ComputeShaderTasks.Dequeue();

            foreach (CSAttribute ca in cst.Attributes)
            {
                ca.Apply(cst.ComputeShader);
            }

            // The problem with AsyncGPUReadback is that .done is probably set in the next frame,
            // .. so we cannot use this to run multiple dispatches during one frame
            //CurrentReadbackRequest = AsyncGPUReadback.Request(cst.FinishedMarkerBuffer);

            cst.ComputeShader.Dispatch(0, cst.ThreadGroups.x, cst.ThreadGroups.y, 1);

            //GL.Flush();

            // Alternative but slow: GetData() blocks until the task is finished
            //cst.FinishedMarkerBuffer.GetData(new int[1]);
            //cst.FinishedMarkerBuffer.Dispose();

            //while (!CurrentReadbackRequest.done)
            //{
            //    Thread.Sleep(1);
            //}

            foreach (ComputeBuffer c in cst.BuffersToDispose)
            {
                c.Dispose();
            }
        }
    }
    
    private void OnDestroy()
    {
        if (Rakel != null)
            Rakel.Dispose();

        if (Canvas != null)
            Canvas.Dispose();
    }

    public void UpdateRakelLength(float worldSpaceLength)
    {
        RakelLength = worldSpaceLength;
        CreateRakelDrawer();
    }

    public void UpdateRakelWidth(float worldSpaceWidth)
    {
        RakelWidth = worldSpaceWidth;
        CreateRakelDrawer();
    }

    public void UpdateRakelRotation(float rotation)
    {
        RakelRotation = rotation;
    }

    public void UpdateRakelPaint(Paint paint)
    {
    //    RakelPaint = paint;
       Rakel.Fill(paint, new PerlinNoiseFiller());
    }

    public void UpdateTextureResolution(int pixelsPerWorldSpaceUnit)
    {
        TextureResolution = pixelsPerWorldSpaceUnit;
        CreateCanvasAndTools();
    }
}