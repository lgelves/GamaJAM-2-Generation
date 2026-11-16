using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MazeImageGenerator : MonoBehaviour
{
    [Header("Imagen de entrada")]
    public Texture2D mazeImage;

    [Header("Dimensiones")]
    [Min(0.1f)] public float wallHeight = 5f;
    [Min(0.01f)] public float wallThickness = 1f;
    [Min(0.001f)] public float floorThickness = 0.05f;

    [Header("Materiales opcionales")]
    public Material wallMaterial;
    public Material mainPathMaterial;
    public Material alternativePathMaterial;

    [Header("Reconocimiento de colores")]
    [Range(0, 80)] public int colorTolerance = 18;

    private const int MaxBoxesPerMesh = 6000;

    private enum CellType : byte
    {
        Empty,
        Wall,
        MainPath,
        AlternativePath
    }

    [ContextMenu("Generate Maze")]
    public void GenerateMaze()
    {
        if (mazeImage == null)
        {
            Debug.LogError("Asigna una imagen en Maze Image Generator.", this);
            return;
        }

        if (!mazeImage.isReadable)
        {
            Debug.LogError("La imagen no permite lectura. Activa Read/Write en sus Import Settings y pulsa Apply.", mazeImage);
            return;
        }

        DeletePreviousMaze();

        int width = mazeImage.width;
        int height = mazeImage.height;
        Color32[] pixels = mazeImage.GetPixels32();
        CellType[,] cells = new CellType[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 pixel = pixels[y * width + x];

                if (IsColor(pixel, new Color32(106, 165, 65, 255)))
                    cells[x, y] = CellType.MainPath; // Verde
                else if (IsColor(pixel, new Color32(78, 30, 96, 255)))
                    cells[x, y] = CellType.AlternativePath; // Morado
                else
                    cells[x, y] = CellType.Wall; // Café, rojo y píxeles fuera de la paleta
            }
        }

        Transform root = transform;
        CreateMergedGeometry(cells, width, height, CellType.Wall, wallHeight, wallMaterial, "Paredes", root);
        CreateMergedGeometry(cells, width, height, CellType.MainPath, floorThickness, mainPathMaterial, "Ruta_Principal", root);
        CreateMergedGeometry(cells, width, height, CellType.AlternativePath, floorThickness, alternativePathMaterial, "Rutas_Alternativas", root);

        Debug.Log($"Laberinto generado desde una imagen de {width} x {height} píxeles. Escala: 1 píxel = 1 unidad.", this);
    }

    private bool IsColor(Color32 actual, Color32 target)
    {
        int dr = actual.r - target.r;
        int dg = actual.g - target.g;
        int db = actual.b - target.b;
        return dr * dr + dg * dg + db * db <= colorTolerance * colorTolerance;
    }

    private void CreateMergedGeometry(
        CellType[,] cells,
        int width,
        int height,
        CellType targetType,
        float verticalSize,
        Material material,
        string objectName,
        Transform parent)
    {
        bool[,] visited = new bool[width, height];
        List<Vector3> vertices = new List<Vector3>();
        List<Face> faces = new List<Face>();
        int boxCount = 0;
        int chunkIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (visited[x, y] || cells[x, y] != targetType)
                    continue;

                int rectangleWidth = 1;
                while (x + rectangleWidth < width &&
                       !visited[x + rectangleWidth, y] &&
                       cells[x + rectangleWidth, y] == targetType)
                {
                    rectangleWidth++;
                }

                int rectangleHeight = 1;
                bool canGrow = true;

                while (y + rectangleHeight < height && canGrow)
                {
                    for (int offsetX = 0; offsetX < rectangleWidth; offsetX++)
                    {
                        if (visited[x + offsetX, y + rectangleHeight] ||
                            cells[x + offsetX, y + rectangleHeight] != targetType)
                        {
                            canGrow = false;
                            break;
                        }
                    }

                    if (canGrow)
                        rectangleHeight++;
                }

                for (int offsetY = 0; offsetY < rectangleHeight; offsetY++)
                {
                    for (int offsetX = 0; offsetX < rectangleWidth; offsetX++)
                        visited[x + offsetX, y + offsetY] = true;
                }

                AddBox(
                    vertices,
                    faces,
                    x,
                    y,
                    rectangleWidth,
                    rectangleHeight,
                    verticalSize);

                boxCount++;

                if (boxCount >= MaxBoxesPerMesh)
                {
                    CreateProBuilderChunk(vertices, faces, parent, objectName, chunkIndex++, material);
                    vertices.Clear();
                    faces.Clear();
                    boxCount = 0;
                }
            }
        }

        if (boxCount > 0)
            CreateProBuilderChunk(vertices, faces, parent, objectName, chunkIndex, material);
    }

    private void AddBox(
        List<Vector3> vertices,
        List<Face> faces,
        float x,
        float z,
        float width,
        float depth,
        float height)
    {
        int start = vertices.Count;

        // Ocho vértices de un prisma rectangular.
        vertices.Add(new Vector3(x, 0, z));
        vertices.Add(new Vector3(x + width, 0, z));
        vertices.Add(new Vector3(x + width, 0, z + depth));
        vertices.Add(new Vector3(x, 0, z + depth));

        vertices.Add(new Vector3(x, height, z));
        vertices.Add(new Vector3(x + width, height, z));
        vertices.Add(new Vector3(x + width, height, z + depth));
        vertices.Add(new Vector3(x, height, z + depth));

        // Cada cara contiene dos triángulos.
        AddQuad(faces, start, 0, 3, 2, 1); // Inferior
        AddQuad(faces, start, 4, 5, 6, 7); // Superior
        AddQuad(faces, start, 0, 1, 5, 4); // Frente
        AddQuad(faces, start, 1, 2, 6, 5); // Derecha
        AddQuad(faces, start, 2, 3, 7, 6); // Fondo
        AddQuad(faces, start, 3, 0, 4, 7); // Izquierda
    }

    private void AddQuad(List<Face> faces, int start, int a, int b, int c, int d)
    {
        faces.Add(new Face(new int[]
        {
            start + a, start + b, start + c,
            start + a, start + c, start + d
        }));
    }

    private void CreateProBuilderChunk(
        List<Vector3> vertices,
        List<Face> faces,
        Transform parent,
        string objectName,
        int chunkIndex,
        Material material)
    {
        ProBuilderMesh mesh = ProBuilderMesh.Create(vertices, faces);
        mesh.gameObject.name = $"{objectName}_{chunkIndex:000}";
        mesh.transform.SetParent(parent, false);
        mesh.ToMesh();
        mesh.Refresh();

        MeshRenderer meshRenderer = mesh.GetComponent<MeshRenderer>();
        if (material != null)
            meshRenderer.sharedMaterial = material;

        MeshCollider meshCollider = mesh.gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
    }

    private void DeletePreviousMaze()
    {
        List<GameObject> previousObjects = new List<GameObject>();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child.name.StartsWith("Paredes_") ||
                child.name.StartsWith("Ruta_Principal_") ||
                child.name.StartsWith("Rutas_Alternativas_"))
            {
                previousObjects.Add(child.gameObject);
            }
        }

        foreach (GameObject item in previousObjects)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(item);
            else
                Destroy(item);
#else
            Destroy(item);
#endif
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MazeImageGenerator))]
public class MazeImageGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        MazeImageGenerator generator = (MazeImageGenerator)target;

        GUI.backgroundColor = new Color(0.45f, 0.8f, 0.5f);
        if (GUILayout.Button("GENERATE MAZE", GUILayout.Height(35)))
        {
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Maze");
            generator.GenerateMaze();
            EditorUtility.SetDirty(generator);
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif
