using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

public static class SkyColiseumRuinsBuilder
{
    private const string Root = "Assets/Art/SkyColiseum/";
    private static System.Random _random;
    private static Material _marble, _stone, _floor, _gold, _crack, _moss, _leaf, _bark;
    private static float Next(float a, float b) { return Mathf.Lerp(a,b,(float)_random.NextDouble()); }
    private static Transform SceneRoot(string name)
    {
        foreach(var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) if(g.name==name)return g.transform;
        return null;
    }
    private static Transform Group(string name)
    {
        var old=SceneRoot(name); if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
        var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Build ruins");return g.transform;
    }
    private static T Store<T>(T item,string path) where T:UnityEngine.Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old==null){AssetDatabase.CreateAsset(item,path);return item;}
        EditorUtility.CopySerialized(item,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(item);return old;
    }
    private static Material Material(string name,Color color)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.06f);
        return Store(m,Root+"Materials/"+name+".mat");
    }
    private static Texture2D WriteTexture(string name,Color[] pixels,bool normal=false)
    {
        var t=new Texture2D(1024,1024,TextureFormat.RGBA32,false);t.SetPixels(pixels);t.Apply();string p=Root+"Textures/"+name+".png";File.WriteAllBytes(p,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(p);
        var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=1024;importer.isReadable=false;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
    private static Material Aged(string name,Color baseColor)
    {
        var pixels=new Color[1024*1024];var heights=new float[pixels.Length];var masks=new Color[pixels.Length];
        for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)
        {
            float u=x/1024f,v=y/1024f;float coarse=Mathf.PerlinNoise(u*7+19,v*7+23),fine=Mathf.PerlinNoise(u*130+43,v*130+17);float grain=Mathf.PerlinNoise(u*380,v*380);
            float vein=Mathf.Pow(1-Mathf.Abs(Mathf.Sin((u*6+v*3+Mathf.PerlinNoise(u*5,v*5)*1.5f)*Mathf.PI)),20);
            float stain=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.82f,coarse));float moss=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,.82f,Mathf.PerlinNoise(u*5+7,v*5+71)));
            var c=baseColor*(.84f+.16f*fine);c=Color.Lerp(c,new Color(.32f,.28f,.19f),stain*.24f+vein*.22f);c=Color.Lerp(c,new Color(.22f,.29f,.10f),moss*.8f);c.a=1;
int i=y*1024+x;pixels[i]=c;heights[i]=fine*.08f+grain*.025f-vein*.07f;masks[i]=new Color(moss,moss,moss,1);
        }
        var normals=new Color[pixels.Length];for(int y=0;y<1024;y++)for(int x=0;x<1024;x++){int i=y*1024+x;float dx=heights[y*1024+(x+1)%1024]-heights[y*1024+(x+1023)%1024],dy=heights[((y+1)%1024)*1024+x]-heights[((y+1023)%1024)*1024+x];var n=new Vector3(-dx*8,-dy*8,1).normalized;normals[i]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);}
        var m=Material(name,Color.white);m.SetTexture("_BaseMap",WriteTexture(name+"_Albedo",pixels));m.SetTexture("_BumpMap",WriteTexture(name+"_Normal",normals,true));WriteTexture(name+"_MossMask",masks);m.SetFloat("_BumpScale",.65f);m.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(m);return m;
    }
    [MenuItem("Tools/Sky Coliseum/1 - Age surfaces")]
    public static void AgeSurfaces()
    {
        if(SceneRoot("Arena")==null)throw new System.InvalidOperationException("Open SkyColiseum first.");
        foreach(string d in new[]{"Textures","Meshes/Ruins","Prefabs/Ruins","Prefabs/Vegetation","Materials"})Directory.CreateDirectory(Root+d);AssetDatabase.Refresh();
        _marble=Aged("AgedMarble",new Color(.86f,.83f,.72f));_stone=Aged("AgedSandstone",new Color(.63f,.56f,.42f));_floor=Aged("AgedArenaStone",new Color(.79f,.74f,.63f));_gold=Aged("TarnishedGold",new Color(.58f,.43f,.21f));_gold.SetFloat("_Metallic",.35f);
        foreach(string rootName in new[]{"Arena","FloatingFoundation","Seating_240_degrees","Columns","Entrances_Two"})
        foreach(var mf in SceneRoot(rootName).GetComponentsInChildren<MeshFilter>())
        {
            var renderer=mf.GetComponent<MeshRenderer>();var old=renderer.sharedMaterial;Undo.RecordObject(renderer,"Age marble");
            if(old.name=="Marble"||old.name=="AgedMarble")renderer.sharedMaterial=_marble;else if(old.name=="Sandstone"||old.name=="AgedSandstone")renderer.sharedMaterial=_stone;else if(old.name=="ArenaStone"||old.name=="AgedArenaStone")renderer.sharedMaterial=_floor;else if(old.name=="Gold"||old.name=="TarnishedGold")renderer.sharedMaterial=_gold;
            if(mf.sharedMesh.uv.Length!=0)continue;
            var mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=mf.sharedMesh.name+"_Weathered";MapUV(mesh);string source=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mf.sharedMesh));var saved=Store(mesh,Root+"Meshes/Ruins/"+source+"_UV.asset");Undo.RecordObject(mf,"Add stone UV");mf.sharedMesh=saved;var col=mf.GetComponent<MeshCollider>();if(col!=null){Undo.RecordObject(col,"Update stone collider");col.sharedMesh=saved;}
        }
        Save();
    }
    private static void MapUV(UnityEngine.Mesh mesh)
    {
        var v=mesh.vertices;var n=mesh.normals;var uv=new Vector2[v.Length];for(int i=0;i<v.Length;i++){var a=n[i];if(Mathf.Abs(a.y)>.5f)uv[i]=new Vector2(v[i].x,v[i].z)/4;else if(Mathf.Abs(a.x)>Mathf.Abs(a.z))uv[i]=new Vector2(v[i].z,v[i].y)/4;else uv[i]=new Vector2(v[i].x,v[i].y)/4;}mesh.uv=uv;mesh.RecalculateTangents();
    }
    private static void LoadMaterials()
    {
        _marble=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/AgedMarble.mat");_stone=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/AgedSandstone.mat");
        _crack=Material("DeepFractures",new Color(.12f,.105f,.07f));_moss=Material("LivingMoss",new Color(.22f,.28f,.10f));_leaf=Material("OliveLeaves",new Color(.30f,.40f,.16f));_leaf.SetFloat("_Cull",0);_bark=Material("OldBark",new Color(.23f,.18f,.11f));
    }
    private static void Tri(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2);}
    private static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(v,t,a,b,c);Tri(v,t,a,c,d);}
    private static UnityEngine.Mesh Mesh(List<Vector3> v,List<int> t,string name)
    {
        var m=new UnityEngine.Mesh();if(v.Count>65000)m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.name=name;m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();MapUV(m);return Store(m,Root+"Meshes/Ruins/"+name+".asset");
    }
    private static GameObject Piece(string name,UnityEngine.Mesh mesh,Material mat,Transform parent,bool collider=false)
    {
        var g=new GameObject(name);Undo.RegisterCreatedObjectUndo(g,"Build ruin piece");g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;if(collider)g.AddComponent<MeshCollider>().sharedMesh=mesh;return g;
    }
    private static Vector3 Polar(float angle,float radius,float y=0){float a=angle*Mathf.Deg2Rad;return new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius);}
    private static float BrokenRadius(float a,float original)
    {
        float angle=Mathf.Repeat(a,360);if(angle<=120||angle>=240)return original;
        float envelope=Mathf.Sin((angle-120)/120*Mathf.PI);
        float bite=4.2f+4.8f*Mathf.Pow(Mathf.Abs(Mathf.Sin(angle*.13f)),5)+Mathf.PerlinNoise(angle*.8f,11)*1.8f;
        return Mathf.Min(original,27.8f-envelope*bite);
    }
    private static GameObject Prefab(GameObject g,string folder,string name){var p=PrefabUtility.SaveAsPrefabAsset(g,Root+"Prefabs/"+folder+"/"+name+".prefab");Undo.DestroyObjectImmediate(g);return p;}
    private static GameObject ColumnPrefab(float h,string name)
    {
        if(name=="FallenFlutedSegment"||name=="BrokenPillarShort"||name=="BrokenPillarMedium"||name=="BrokenPillarTall")return SolidFallenPrefab(h,name,name=="BrokenPillarMedium"?45:35);
        var v=new List<Vector3>();var t=new List<int>();int n=48;var caps=new Vector3[n];
for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;float radius=.52f+Mathf.Cos(a*12)*.055f;caps[i]=new Vector3(Mathf.Sin(a)*radius,h+Next(-.36f,.24f),Mathf.Cos(a)*radius);}
        for(int i=0;i<n;i++){int j=(i+1)%n;var a=caps[i];var b=caps[j];var bottomA=new Vector3(a.x,0,a.z);var bottomB=new Vector3(b.x,0,b.z);Quad(v,t,bottomB,b,a,bottomA);Tri(v,t,b,new Vector3(.09f,h-.19f,-.08f),a);Tri(v,t,bottomA,Vector3.zero,bottomB);}
        var g=Piece(name,Mesh(v,t,name),_marble,null,true);return Prefab(g,"Ruins",name);
    }
private static GameObject SolidFallenPrefab(float h,string name="FallenFlutedSegment",float cutAngle=35)
{
    var v=new List<Vector3>();var t=new List<int>();var normals=new List<Vector3>();var uv=new List<Vector2>();const int segments=96;float slope=Mathf.Tan(cutAngle*Mathf.Deg2Rad);
for(int i=0;i<segments;i++)
    {
        float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;float ra=.52f+.025f*Mathf.Cos(12*a),rb=.52f+.025f*Mathf.Cos(12*b);
        var loA=new Vector3(Mathf.Sin(a)*ra,0,Mathf.Cos(a)*ra);var loB=new Vector3(Mathf.Sin(b)*rb,0,Mathf.Cos(b)*rb);var hiA=loA+Vector3.up*(h+loA.x*slope);var hiB=loB+Vector3.up*(h+loB.x*slope);
        int k=v.Count;Quad(v,t,loB,hiB,hiA,loA);
        // Derivative of the fluted radius gives a continuous outward normal.
        float da=-.3f*Mathf.Sin(12*a),db=-.3f*Mathf.Sin(12*b);var na=new Vector3(ra*Mathf.Sin(a)-da*Mathf.Cos(a),0,ra*Mathf.Cos(a)+da*Mathf.Sin(a)).normalized;var nb=new Vector3(rb*Mathf.Sin(b)-db*Mathf.Cos(b),0,rb*Mathf.Cos(b)+db*Mathf.Sin(b)).normalized;
        normals.AddRange(new[]{nb,nb,na,nb,na,na});float uA=(float)i/segments,uB=(float)(i+1)/segments;uv.AddRange(new[]{new Vector2(uB,0),new Vector2(uB,hiB.y/3),new Vector2(uA,hiA.y/3),new Vector2(uB,0),new Vector2(uA,hiA.y/3),new Vector2(uA,0)});
        Tri(v,t,hiB,new Vector3(0,h,0),hiA);var topNormal=new Vector3(-slope,1,0).normalized;normals.AddRange(new[]{topNormal,topNormal,topNormal});uv.AddRange(new[]{new Vector2(hiB.x,hiB.z),Vector2.zero,new Vector2(hiA.x,hiA.z)});
        Tri(v,t,loA,Vector3.zero,loB);normals.AddRange(new[]{Vector3.down,Vector3.down,Vector3.down});uv.AddRange(new[]{new Vector2(loA.x,loA.z),Vector2.zero,new Vector2(loB.x,loB.z)});
    }
    var m=new UnityEngine.Mesh();m.name=name;m.SetVertices(v);m.SetTriangles(t,0);m.SetNormals(normals);m.SetUVs(0,uv);m.RecalculateBounds();m.RecalculateTangents();var saved=Store(m,Root+"Meshes/Ruins/"+name+"Solid.asset");
    var mat=new Material(_marble);mat.name="SolidColumnMarble";mat.DisableKeyword("_NORMALMAP");mat.SetTexture("_BumpMap",null);mat.SetFloat("_Surface",0);mat.SetFloat("_Cull",2);mat.SetFloat("_ZWrite",1);mat.SetOverrideTag("RenderType","Opaque");mat.renderQueue=2000;mat=Store(mat,Root+"Materials/SolidColumnMarble.mat");
    return Prefab(Piece(name,saved,mat,null,true),"Ruins",name);
}
[MenuItem("Tools/Sky Coliseum/Repair tall pillar and clear combat moss")]
public static void RepairTallAndMoss()
{
    _marble=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/AgedMarble.mat");SolidFallenPrefab(3.7f,"BrokenPillarTall",35);
    foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())foreach(var mf in root.GetComponentsInChildren<MeshFilter>()){
        if(mf.name=="BrokenPillarTall"){
            var mesh=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(Root+"Meshes/Ruins/BrokenPillarTallSolid.asset");Undo.RecordObject(mf,"Solid tall pillar");mf.sharedMesh=mesh;var mr=mf.GetComponent<MeshRenderer>();Undo.RecordObject(mr,"Opaque tall pillar");mr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/SolidColumnMarble.mat");var col=mf.GetComponent<MeshCollider>();if(col!=null){Undo.RecordObject(col,"Tall pillar collision");col.sharedMesh=null;col.sharedMesh=mesh;}
        }
        if(mf.name=="Irregular moss from joints and fissures"){
            var source=mf.sharedMesh;var v=source.vertices;var triangles=source.triangles;var kept=new List<int>();
            for(int i=0;i<triangles.Length;i+=3){bool keep=true;for(int k=0;k<3;k++){var world=mf.transform.TransformPoint(v[triangles[i+k]]);if(new Vector2(world.x,world.z).magnitude<16.5f||world.y<.3f)keep=false;}if(keep){kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);}}
            // Compact the mesh so excluded vertices cannot reappear in bounds or later edits.
            var nv=new List<Vector3>();var nt=new List<int>();foreach(int index in kept){nt.Add(nv.Count);nv.Add(v[index]);}Undo.RecordObject(mf,"Keep moss on seating only");mf.sharedMesh=Mesh(nv,nt,"EmergentMossOutsideCombat");
        }
    }
    Save();
}
[MenuItem("Tools/Sky Coliseum/Repair short and medium pillars")]
public static void RepairShortMedium()
{
_marble=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/AgedMarble.mat");SolidFallenPrefab(1.2f,"BrokenPillarShort",35);SolidFallenPrefab(2.5f,"BrokenPillarMedium",45);
    foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())foreach(var mf in root.GetComponentsInChildren<MeshFilter>())if(mf.name=="BrokenPillarShort"||mf.name=="BrokenPillarMedium"){
        var mesh=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(Root+"Meshes/Ruins/"+mf.name+"Solid.asset");Undo.RecordObject(mf,"Solid pillar mesh");mf.sharedMesh=mesh;var mr=mf.GetComponent<MeshRenderer>();Undo.RecordObject(mr,"Solid pillar material");mr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/SolidColumnMarble.mat");var col=mf.GetComponent<MeshCollider>();if(col!=null){Undo.RecordObject(col,"Solid pillar collision");col.sharedMesh=null;col.sharedMesh=mesh;}
    }
    Save();
}
[MenuItem("Tools/Sky Coliseum/Repair FallenFlutedSegment only")]
public static void RepairFallenSegment()
{
_marble=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/AgedMarble.mat");SolidFallenPrefab(3.3f);var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/SolidColumnMarble.mat");var mesh=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(Root+"Meshes/Ruins/FallenFlutedSegmentSolid.asset");
    foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())foreach(var mf in root.GetComponentsInChildren<MeshFilter>())if(mf.name=="FallenFlutedSegment"){Undo.RecordObject(mf,"Replace fallen mesh");mf.sharedMesh=mesh;Undo.RecordObject(mf.GetComponent<MeshRenderer>(),"Repair solid column");mf.GetComponent<MeshRenderer>().sharedMaterial=mat;var col=mf.GetComponent<MeshCollider>();if(col!=null){Undo.RecordObject(col,"Refresh column collision");col.sharedMesh=null;col.sharedMesh=mesh;}}
    Save();
}
private static void Place(GameObject prefab,Transform parent,Vector3 p,Quaternion q,float scale=1)
{
var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);Undo.RegisterCreatedObjectUndo(g,"Place ruins");g.transform.SetParent(parent);g.transform.position=p;g.transform.rotation=q;g.transform.localScale=Vector3.one*scale;
    }
    private static float Surface(Vector3 p)
    {
        RaycastHit hit;if(Physics.Raycast(new Vector3(p.x,20,p.z),Vector3.down,out hit,30)&&hit.normal.y>.8f)return hit.point.y;return float.NaN;
    }
    private static bool KeepClear(Vector3 p)
    {
        float a=Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg;float radius=new Vector2(p.x,p.z).magnitude;
        if(radius>14&&Mathf.Abs(Mathf.Abs(a)-70)<10)return true;
        if(radius>16.2f&&radius<27&& (Mathf.Abs(a)<3||Mathf.Abs(Mathf.Abs(a)-105)<3))return true;
        return false;
    }
    private static readonly List<Vector3> _growthSeeds=new List<Vector3>();
    private static void CrackPath(List<Vector3> v,List<int> t,Vector3 start,Vector3 direction,int count,float width)
    {
        var p=start;for(int i=0;i<count;i++){var q=p+direction*Next(.35f,.7f)+new Vector3(Next(-.23f,.23f),0,Next(-.23f,.23f));float y=Surface(p),z=Surface(q);if(float.IsNaN(y)||float.IsNaN(z)||KeepClear(p)||new Vector2(p.x,p.z).magnitude>26.3f){p=q;continue;}var side=Vector3.Cross(Vector3.up,(q-p).normalized)*width*Next(.3f,1);var a=new Vector3(p.x,y+.014f,p.z);var b=new Vector3(q.x,z+.014f,q.z);if(Mathf.Abs(y-z)<.1f)Quad(v,t,a-side,b-side,b+side,a+side);else{var midpoint=(a+b)*.5f;var hi=new Vector3(midpoint.x,Mathf.Max(y,z)+.014f,midpoint.z);var lo=new Vector3(midpoint.x,Mathf.Min(y,z)+.014f,midpoint.z);Quad(v,t,a-side,hi-side,hi+side,a+side);Quad(v,t,hi-side,lo-side,lo+side,hi+side);Quad(v,t,lo-side,b-side,b+side,lo+side);}if(i%3==0&&new Vector2(p.x,p.z).magnitude>7)_growthSeeds.Add(a);if(i==count/2&&count>8)CrackPath(v,t,p,Quaternion.Euler(0,Next(30,65),0)*direction,count/3,width*.6f);p=q;}
    }
    [MenuItem("Tools/Sky Coliseum/2 - Fractures and broken columns")]
    public static void BuildRuins()
    {
        _random=new System.Random(71);LoadMaterials();var ruins=Group("Ruins");var cracks=Group("SurfaceCracks");
        foreach(var mf in SceneRoot("FloatingFoundation").GetComponentsInChildren<MeshFilter>())
        {
            // Always derive the fracture from the intact original, never repeatedly shrink an already fractured rim.
            string id=mf.name=="Upper plinth"?"Part0":mf.name=="Golden cornice"?"Part1":mf.name=="Floating foundation"?"Part2":mf.name=="Lower platform"?"Part3":"Part4";
            var source=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(Root+"Meshes/"+id+".asset");var m=UnityEngine.Object.Instantiate(source);var v=m.vertices;
            for(int i=0;i<v.Length;i++){float radius=new Vector2(v[i].x,v[i].z).magnitude;if(radius<.01f)continue;float a=Mathf.Atan2(v[i].x,v[i].z)*Mathf.Rad2Deg;float cut=BrokenRadius(a,radius);v[i]=new Vector3(v[i].x*cut/radius,v[i].y,v[i].z*cut/radius);}m.vertices=v;m.RecalculateNormals();m.RecalculateBounds();MapUV(m);var saved=Store(m,Root+"Meshes/Ruins/"+id+"_Fractured.asset");Undo.RecordObject(mf,"Fracture platform");mf.sharedMesh=saved;var col=mf.GetComponent<MeshCollider>();if(col!=null){Undo.RecordObject(col,"Fracture collision");col.sharedMesh=saved;}
        }
        Physics.SyncTransforms();var standing=new[]{ColumnPrefab(1.2f,"BrokenPillarShort"),ColumnPrefab(2.5f,"BrokenPillarMedium"),ColumnPrefab(3.7f,"BrokenPillarTall")};var fallen=ColumnPrefab(3.3f,"FallenFlutedSegment");
        for(int i=0;i<5;i++){float angle=new[]{-47f,45f,130f,225f,25f}[i];var p=Polar(angle,Next(11,14));p.y=Surface(p);Place(standing[i%3],ruins,p,Quaternion.Euler(0,Next(0,360),0));}
        for(int i=0;i<6;i++){var p=Polar(new[]{-30f,35f,-95f,95f,155f,210f}[i],Next(10,14));p.y=Surface(p)+.59f;Place(fallen,ruins,p,Quaternion.Euler(0,Next(0,360),90));}
        for(int i=0;i<3;i++){var p=Polar(new[]{-37f,30f,87f}[i],new[]{21.5f,23.6f,19.4f}[i]);p.y=Surface(p);Place(standing[i%3],ruins,p,Quaternion.Euler(0,Next(0,360),0),.85f);}
        for(int i=0;i<22;i++){var p=Polar(Next(133,230),Next(20,27),Next(-10,-5));Place(standing[0],ruins,p,Quaternion.Euler(Next(30,160),Next(0,360),Next(0,180)),Next(.55f,1.8f));}
        _growthSeeds.Clear();var cv=new List<Vector3>();var ct=new List<int>();for(int i=0;i<52;i++){float a=Next(-118,118),radius=Next(8,25);if(i>35)a=Next(122,238);var p=Polar(a,radius);CrackPath(cv,ct,p,Polar(a+Next(-65,65),1),_random.Next(9,19),Next(.025f,.075f));}CrackPath(cv,ct,new Vector3(-3,0,-2),new Vector3(1,0,.35f).normalized,10,.02f);Piece("Branching floor and seating fissures",Mesh(cv,ct,"SurfaceFissures"),_crack,cracks);Save();
    }
    [MenuItem("Tools/Sky Coliseum/Extra - Chipped steps and capitals")]
    public static void BuildDetails()
    {
        _random=new System.Random(241);LoadMaterials();var details=Group("RuinsDetails");
        int tierIndex=0;
        foreach(var mf in SceneRoot("Seating_240_degrees").GetComponentsInChildren<MeshFilter>())
        {
            if(!mf.name.StartsWith("Tier ")||mf.name=="Tier trim")continue;
            tierIndex++;if(tierIndex!=5&&tierIndex!=13&&tierIndex!=22)continue;
            string id=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mf.sharedMesh)).Split('_')[0];
            var source=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(Root+"Meshes/"+id+".asset");if(source==null)continue;
            var m=UnityEngine.Object.Instantiate(source);var v=m.vertices;var center=Polar(tierIndex==5?-25:tierIndex==13?-90:-115,new Vector2(v[0].x,v[0].z).magnitude);
            for(int i=0;i<v.Length;i++){float distance=new Vector2(v[i].x-center.x,v[i].z-center.z).magnitude;if(distance<1.0f&&v[i].y>0)v[i].y-=.22f*(1-distance)*(.5f+Mathf.PerlinNoise(v[i].x*9,v[i].z*9));}
m.vertices=v;m.RecalculateNormals();m.RecalculateBounds();MapUV(m);var saved=Store(m,Root+"Meshes/Ruins/"+id+"_Chipped.asset");Undo.RecordObject(mf,"Chip seating edge");mf.sharedMesh=saved;var col=mf.GetComponent<MeshCollider>();if(col!=null){Undo.RecordObject(col,"Chip seating collider");col.sharedMesh=saved;}
        }
        var vtx=new List<Vector3>();var tri=new List<int>();var corners=new[]{new Vector3(-.7f,0,-.65f),new Vector3(.66f,.06f,-.64f),new Vector3(.7f,0,.6f),new Vector3(-.62f,0,.68f),new Vector3(-.62f,.38f,-.57f),new Vector3(.55f,.51f,-.6f),new Vector3(.61f,.43f,.51f),new Vector3(-.57f,.35f,.57f)};
        Quad(vtx,tri,corners[4],corners[7],corners[6],corners[5]);Quad(vtx,tri,corners[0],corners[1],corners[2],corners[3]);for(int i=0;i<4;i++){int j=(i+1)%4;Quad(vtx,tri,corners[i],corners[i+4],corners[j+4],corners[j]);}
        var capital=Prefab(Piece("ChippedCapital",Mesh(vtx,tri,"ChippedCapital"),_marble,null,true),"Ruins","ChippedCapital");
        Physics.SyncTransforms();for(int i=0;i<8;i++){var p=Polar(new[]{-40f,35f,130f,215f,-22f,50f,145f,200f}[i],Next(9.5f,14.5f));float y=Surface(p);if(float.IsNaN(y))continue;p.y=y;Place(capital,details,p,Quaternion.Euler(0,Next(0,360),0),Next(.55f,1));}Save();
    }
    private static void Save(){AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());}
private static void Leaf(List<Vector3> v,List<int> t,Vector3 basePoint,Vector3 tip,float width)
    {
        var side=Vector3.Cross((tip-basePoint).normalized,Vector3.up);if(side.sqrMagnitude<.01f)side=Vector3.right;side=side.normalized*width;var middle=Vector3.Lerp(basePoint,tip,.46f);Tri(v,t,basePoint,middle+side,tip);Tri(v,t,basePoint,tip,middle-side);
    }
    private static GameObject PlantPrefab(string name,int blades,float h,bool fern)
    {
        var v=new List<Vector3>();var t=new List<int>();for(int i=0;i<blades;i++){float a=Next(0,360);var p=Polar(a,Next(.04f,.22f));var tip=p+Polar(a,Next(.15f,.48f),Next(h*.5f,h));Leaf(v,t,p,tip,fern?.10f:.035f);if(fern)for(int j=1;j<5;j++){var b=Vector3.Lerp(p,tip,j/5f);Leaf(v,t,b,b+Polar(a+65,.24f,.03f),.045f);Leaf(v,t,b,b+Polar(a-65,.24f,.03f),.045f);}}return Prefab(Piece(name,Mesh(v,t,name),_leaf,null),"Vegetation",name);
    }
    private static void Branch(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,float r1,float r2)
    {
        var direction=(b-a).normalized;var x=Vector3.Cross(direction,Vector3.forward).normalized;if(x.sqrMagnitude<.01f)x=Vector3.right;var z=Vector3.Cross(direction,x);for(int i=0;i<8;i++){float n=i*Mathf.PI/4,k=(i+1)*Mathf.PI/4;var u=x*Mathf.Sin(n)+z*Mathf.Cos(n);var w=x*Mathf.Sin(k)+z*Mathf.Cos(k);Quad(v,t,a+w*r1,b+w*r2,b+u*r2,a+u*r1);}
    }
    private static GameObject TreePrefab(string name,float h)
    {
        var v=new List<Vector3>();var t=new List<int>();var lv=new List<Vector3>();var lt=new List<int>();var top=new Vector3(.25f,h,0);Branch(v,t,Vector3.zero,top,.15f,.025f);
        for(int i=0;i<7;i++){float angle=i*137;var origin=Vector3.Lerp(Vector3.zero,top,.3f+i*.085f);var end=origin+Polar(angle,Next(.8f,1.7f),Next(.5f,1.3f));Branch(v,t,origin,end,.065f,.012f);for(int k=0;k<45;k++){var p=Vector3.Lerp(origin,end,Next(.5f,1))+new Vector3(Next(-.65f,.65f),Next(-.25f,.55f),Next(-.65f,.65f));Leaf(lv,lt,p,p+Polar(Next(0,360),Next(.22f,.4f),Next(-.1f,.14f)),.065f);}}
        for(int i=0;i<5;i++)Branch(v,t,Polar(i*72,.55f,.03f),Vector3.up*.4f,.05f,.08f);
        var g=Piece(name,Mesh(v,t,name+"Trunk"),_bark,null);Piece("Fine olive foliage",Mesh(lv,lt,name+"Leaves"),_leaf,g.transform);var col=g.AddComponent<CapsuleCollider>();col.radius=.16f;col.height=h;col.center=Vector3.up*h/2;return Prefab(g,"Vegetation",name);
    }
    [MenuItem("Tools/Sky Coliseum/3 - Vegetation 70-30")]
    public static void BuildVegetation()
    {
        _random=new System.Random(117);LoadMaterials();var vegetation=Group("Vegetation");Physics.SyncTransforms();var grass=PlantPrefab("CrackGrass",23,.58f,false);var fern=PlantPrefab("EmergingFern",9,.72f,true);var leaves=PlantPrefab("LeafCluster",14,.26f,true);var trees=new[]{TreePrefab("SlenderOliveA",4.8f),TreePrefab("SlenderOliveB",5.7f),TreePrefab("SlenderOliveC",4.1f)};
        // Use saved fissures rather than static memory, so regeneration survives a domain reload.
        var fissure=SceneRoot("SurfaceCracks").GetComponentInChildren<MeshFilter>().sharedMesh.vertices;var seeds=new List<Vector3>();for(int i=0;i<fissure.Length;i+=18)if(new Vector2(fissure[i].x,fissure[i].z).magnitude>7&&!KeepClear(fissure[i]))seeds.Add(fissure[i]);
        var mv=new List<Vector3>();var mt=new List<int>();float area=0;int plantCount=0;
        for(int i=0;i<650;i++)
        {
            Vector3 p;if(i<seeds.Count)p=seeds[i];else{float a=Next(-119,119);if(i%4==0)a=Next(122,238);p=Polar(a,Next(9,26));}
            if(KeepClear(p)||new Vector2(p.x,p.z).magnitude<18.3f)continue;float y=Surface(p);if(float.IsNaN(y)||y>6)continue;p.y=y+.018f;
float radius=Next(.48f,1.35f);for(int k=0;k<12;k++){Vector3 a=p+Polar(k*30,radius*Next(.65f,1)),b=p+Polar((k+1)*30,radius*Next(.65f,1));float ay=Surface(a),by=Surface(b);if(y<.3f||ay<.3f||by<.3f||float.IsNaN(ay)||float.IsNaN(by)||Mathf.Abs(ay-y)>.1f||Mathf.Abs(by-y)>.1f||KeepClear(a)||KeepClear(b))continue;a.y=ay+.019f;b.y=by+.019f;Tri(mv,mt,p,a,b);area+=Vector3.Cross(a-p,b-p).magnitude*.5f;}
            if(i%2==0){Place(i%5==0?fern:i%3==0?leaves:grass,vegetation,p,Quaternion.Euler(0,Next(0,360),0),Next(.8f,1.6f));plantCount++;}
        }
Piece("Irregular moss from joints and fissures",Mesh(mv,mt,"EmergentMossPatches"),_moss,vegetation);
        foreach(var data in new[]{new Vector2(-48,24),new Vector2(45,24.7f),new Vector2(90,22.5f),new Vector2(-112,24)}){var p=Polar(data.x,data.y);p.y=Surface(p);if(!float.IsNaN(p.y))Place(trees[_random.Next(3)],vegetation,p,Quaternion.Euler(0,Next(0,360),0));}
Save();Debug.Log("Vegetation: "+plantCount+" plants, 4 slender trees; projected moss area before overlap "+area.ToString("F0")+" m2. Target coverage is approximate and requires visual review.");
    }
    [MenuItem("Tools/Sky Coliseum/4 - Solid columns and combat arena")]
    public static void RefineCombatArena()
    {
        _random=new System.Random(71);LoadMaterials();ColumnPrefab(1.2f,"BrokenPillarShort");ColumnPrefab(2.5f,"BrokenPillarMedium");ColumnPrefab(3.7f,"BrokenPillarTall");ColumnPrefab(3.3f,"FallenFlutedSegment");
        var pixels=new Color[1024*1024];var normal=new Color[pixels.Length];
        for(int y=0;y<1024;y++)for(int x=0;x<1024;x++){int row=y/256;float u=Mathf.Repeat(x+(row%2)*256,512)/512f,v=(y%256)/256f;float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));float noise=Mathf.PerlinNoise(x*.08f,y*.08f);float block=Mathf.PerlinNoise(Mathf.Floor((x+(row%2)*256)/512f)*19,row*13+7);Color stone=new Color(.84f,.81f,.72f)*( .96f+.04f*noise);stone*=.97f+.06f*block;pixels[y*1024+x]=edge<.015f?new Color(.47f,.43f,.34f):stone;normal[y*1024+x]=new Color(.5f,.5f,1,1);}
        var paving=Material("CombatLightStoneBlocks",Color.white);paving.SetTexture("_BaseMap",WriteTexture("CombatLightStoneBlocks",pixels));paving.SetFloat("_Smoothness",.11f);foreach(var mr in SceneRoot("Arena").GetComponentsInChildren<MeshRenderer>()){if(mr.name=="Arena inlay"||mr.name=="Medallion trim")continue;Undo.RecordObject(mr,"Clear combat paving");mr.sharedMaterial=paving;}
        BuildVegetation();
        var columns=SceneRoot("Columns");var pillars=new List<Transform>();foreach(Transform child in columns)if(child.name=="Doric pillar")pillars.Add(child);
        var damagedAngles=new List<float>();int damaged=0;
        // Ten of the original twenty-five perimeter pillars (40%). Keep entrance portals untouched.
        for(int index=0;index<pillars.Count;index++)
        {
            if(index%5!=1&&index%5!=3)continue;var pillar=pillars[index];var basePiece=pillar.Find("Square base");if(basePiece==null)continue;Vector3 basePoint=basePiece.position-Vector3.up*.15f;float angle=damaged%2==0?35:45,h=damaged%2==0?3.2f:4.1f;
            var vertices=new List<Vector3>();var triangles=new List<int>();int n=48;float slope=Mathf.Tan(angle*Mathf.Deg2Rad);var cap=new Vector3(0,h,0);
            for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;float ra=.43f+.045f*Mathf.Cos(a*12),rb=.43f+.045f*Mathf.Cos(b*12);var loA=new Vector3(Mathf.Sin(a)*ra,.6f,Mathf.Cos(a)*ra);var loB=new Vector3(Mathf.Sin(b)*rb,.6f,Mathf.Cos(b)*rb);var hiA=new Vector3(loA.x,h+loA.x*slope,loA.z);var hiB=new Vector3(loB.x,h+loB.x*slope,loB.z);Quad(vertices,triangles,loB,hiB,hiA,loA);Tri(vertices,triangles,hiB,cap,hiA);Tri(vertices,triangles,loA,new Vector3(0,.6f,0),loB);}
            foreach(Transform part in pillar){if(part.name=="Square base"||part.name=="Base")continue;foreach(var component in part.GetComponents<Component>()){}foreach(var mr in part.GetComponentsInChildren<MeshRenderer>()){Undo.RecordObject(mr,"Fracture perimeter column");mr.enabled=false;}foreach(var col in part.GetComponentsInChildren<Collider>()){Undo.RecordObject(col,"Fracture perimeter collision");col.enabled=false;}}
            var shaft=Piece("Solid broken shaft "+angle+" degrees",Mesh(vertices,triangles,"ArcadeCut"+index),_marble,pillar,true);shaft.transform.position=basePoint;damagedAngles.Add(Mathf.Atan2(basePoint.x,basePoint.z)*Mathf.Rad2Deg);damaged++;
        }
        foreach(var mf in columns.GetComponentsInChildren<MeshFilter>())
        {
            if(mf.name!="Entablature"&&mf.name!="Entablature trim"&&mf.name!="Crown cornice")continue;
            string id=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mf.sharedMesh)).Split('_')[0];var source=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(Root+"Meshes/"+id+".asset");if(source==null)continue;
            var v=source.vertices;var tr=source.triangles;var outputV=new List<Vector3>();var outputT=new List<int>();float rMin=float.MaxValue,rMax=0,minAngle=180,maxAngle=-180;foreach(var p in v){float radius=new Vector2(p.x,p.z).magnitude;rMin=Mathf.Min(rMin,radius);rMax=Mathf.Max(rMax,radius);float a=Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg;minAngle=Mathf.Min(minAngle,a);maxAngle=Mathf.Max(maxAngle,a);}
            for(int i=0;i<tr.Length;i+=3){var center=(v[tr[i]]+v[tr[i+1]]+v[tr[i+2]])/3;float a=Mathf.Atan2(center.x,center.z)*Mathf.Rad2Deg;bool remove=false;foreach(float d in damagedAngles)if(Mathf.Abs(Mathf.DeltaAngle(a,d))<4.1f)remove=true;if(!remove)Tri(outputV,outputT,v[tr[i]],v[tr[i+1]],v[tr[i+2]]);}
            foreach(float d in damagedAngles)foreach(float edge in new[]{d-4.1f,d+4.1f})if(edge>minAngle&&edge<maxAngle){float low=source.bounds.min.y,high=source.bounds.max.y;var a=Polar(edge,rMin,low);var b=Polar(edge,rMax,low);var c=Polar(edge,rMax,high);var e=Polar(edge,rMin,high);if(edge<d)Quad(outputV,outputT,a,b,c,e);else Quad(outputV,outputT,e,c,b,a);}
            Undo.RecordObject(mf,"Break arcade beam");mf.sharedMesh=Mesh(outputV,outputT,id+"_BrokenArcade");
        }
        Save();Debug.Log("Perimeter pillars broken: "+damaged+" / "+pillars.Count+". Combat vegetation excluded up to seating.");
    }
    [MenuItem("Tools/Sky Coliseum/Rebuild entire ruin treatment")]
    public static void Rebuild(){AgeSurfaces();BuildRuins();BuildDetails();BuildVegetation();RefineCombatArena();}
}
