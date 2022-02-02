using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KKAPI;
using KKAPI.Chara;
using UnityEngine;
#if KK
    using KKAPI.MainGame;
#elif HS2
    using AIChara;
#elif AI
    using AIChara;
#endif

namespace KK_TestPlugin
{     
    public class TestPluginCharaController: CharaCustomFunctionController
    {  

        //Determine body mesh name based on sex (0 is male)
        public string BodyMeshName {
            #if KK
                get { return ChaControl.sex == 0 ?  "o_body_a" : "o_body_a"; }
            #elif HS2 || AI
                get { return ChaControl.sex == 0 ?  "o_body_cm" : "o_body_cf"; }
            #endif            
        }        

        //Ignore
        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {

        }

        protected override void Start() 
        { 
            base.Start();
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            TestPlugin.Logger.LogWarning($" Reload() Started");        
        }

        protected override void Update()
        {
            //Trigger on key press configured in plugin config
            if (TestPlugin.TriggerTextureCompute.Value.IsDown())
            {
                TriggerMaterialPaint();
            }
        }






        internal void TriggerMaterialPaint() 
        {
            //White goes out, black in, and grey no change
            var defaultColor = Color.grey;
            var displaceColor = Color.black;
            
            var bodySmr = GetBodyMeshRenderer();
            var bodyTriangles = bodySmr.sharedMesh.triangles;
            CreateMeshCollider(bodySmr);
            var meshCollider = GetMeshCollider(bodySmr);
            var bodyTexture = bodySmr.material.mainTexture;
            var rayMaxDistance = 0.2f;

            //Create new body texture for displacement, that we will paint                    
            var bodyDisplacementText = new Texture2D(bodyTexture.width, bodyTexture.height);            
            //Make it solid color at start
            var fillColorArray = bodyDisplacementText.GetPixels();            
            for(var i = 0; i < fillColorArray.Length; ++i)
            {
                fillColorArray[i] = defaultColor;
            }            
            bodyDisplacementText.SetPixels(fillColorArray);            
            bodyDisplacementText.Apply();

            TestPlugin.Logger.LogInfo($" Body {bodySmr.name} texture {bodyDisplacementText.height}x{bodyDisplacementText.width}   uvs {bodySmr.sharedMesh.vertexCount}  verts {bodySmr.sharedMesh.vertexCount}");
            TestPlugin.Logger.LogInfo($" ");

            //Get all clothing mesh renderers
            var clothRenderers = GetMeshRenderers(ChaControl.objClothes);            
            foreach (var smr in clothRenderers) 
            {
                TestPlugin.Logger.LogInfo($" ");
                //init body Uvs that we want to paint white
                var bodyIsVisibleList = new bool[bodySmr.sharedMesh.vertexCount];
                //Tracks which body verts were hit (computed from triangle index hit)
                var hitUvCoordList = new Vector2[bodySmr.sharedMesh.vertexCount];                

                //Bake mesh to align with the meshCollider
                var bakedMesh = new Mesh();
                smr.BakeMesh(bakedMesh);

                //Get the cloth texture
                RenderTexture rt = (RenderTexture)smr.material.mainTexture;
                Texture2D clothTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);                
                RenderTexture.active = rt;
                clothTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
                RenderTexture.active = null;                
                clothTexture.Apply();

                var uvs = bakedMesh.uv;                
                var verts = bakedMesh.vertices;                
                var normals = bakedMesh.normals;                
                var hitTriList = new int[uvs.Length];
                var hitCount = 0;
                TestPlugin.Logger.LogInfo($" {smr.name} texture {clothTexture.height}x{clothTexture.width}   uvs {uvs.Length}");   
                var clothPixels = clothTexture.GetPixels();
                //For each cloth index, gives the body uv it is above
                var bodyUvClothProjectionWs = new Vector2[clothPixels.Length];
                var bodyHitList = new bool[clothPixels.Length];
                
                // TestPlugin.Logger.LogInfo($" uvCornersWs {uvCornersWs[0]}    {uvCornersWs[1]}");

                //Raycast cloth texture pixels to get matching body texture positions
                for (int w = 0; w < clothTexture.width; w++) 
                {
                    var x = (float)w/clothTexture.width;                
                    for (int h = 0; h < clothTexture.height; h++) 
                    {
                        var y = (float)h/clothTexture.height;
                        var i = w + (h*clothTexture.width);

                        //Convert uv corner xy to worldspace points
                        UvToWorldspace(new Vector2(x, y), bakedMesh, out var uvPositionWs, out var uvNormal);

                        var _direction = -uvNormal;
                        var origin = uvPositionWs;
                        var tryHit = RayCastToMeshCollider(origin, rayMaxDistance, _direction, meshCollider);                        

                        //If we hit the body
                        if (tryHit.distance > 0 && tryHit.distance < rayMaxDistance) 
                        {     
                            hitCount++;         

                            //Get the texture hit
                            bodyUvClothProjectionWs[i] = tryHit.textureCoord;
                            bodyHitList[i] = true;        
                            continue;      
                        }                      
                    }
                }
                TestPlugin.Logger.LogInfo($" {smr.name} hitCount {hitCount}");
                if (hitCount <= 1) continue;

                //loop through each pixel in the cloth texture
                for (int w = 0; w < clothTexture.width; w++) 
                {
                    var x = Mathf.FloorToInt((float)w/clothTexture.width);                
                    for (int h = 0; h < clothTexture.height; h++) 
                    {                        
                        var i = w + (h*clothTexture.width);                        
                        //If the body was not hit, skip
                        if (!bodyHitList[i]) continue;

                        var y = Mathf.FloorToInt((float)h/clothTexture.height);
                        
                        //Convert body UV coords to pixel coords
                        var bodyX = Mathf.FloorToInt(bodyUvClothProjectionWs[i].x * bodyDisplacementText.width);
                        var bodyY = Mathf.FloorToInt(bodyUvClothProjectionWs[i].y * bodyDisplacementText.height);

                        //Check the cloth uv color
                        var color = clothTexture.GetPixel(x, y);
                        var isTransparent = color.a < 0.01f;    

                        //Set the body pixel color that we hit
                        bodyDisplacementText.SetPixel(bodyX, bodyY, isTransparent ? defaultColor : displaceColor);

                    }
                }
                bodyDisplacementText.Apply();            

                //Now do it again for all other cloth meshes
            }      

            //Save the texture as a png to the rootGame directory for debugging
            SaveTexture(bodyDisplacementText);
            TestPlugin.Logger.LogInfo($" ");

            //Then apply that body material to the DisplaceTex shader texture
            var shader = "xukmi/SkinPlusTess";
            for (int i = 0; i < bodySmr.materials.Length; i++)
            {
                if (bodySmr.materials[i]?.shader?.name == null) continue;
                
                //If the shader matches
                if (bodySmr.materials[i].shader.name == shader || bodySmr.materials[i].shader.name.Contains(shader))
                {
                    TestPlugin.Logger.LogInfo($" Shader found {shader}");
                    //Set the new material
                    bodySmr.materials[i].SetTexture("_DisplaceTex", bodyDisplacementText);
                    //Adjust the weight so you can see it
                    bodySmr.materials[i].SetFloat("_DisplaceMultiplier", 0.5f);
                }
            }

            TestPlugin.Logger.LogInfo($" Removing Collider");
            RemoveMeshCollider();
        }






        //Project the cloth color down to the body texture
        public Color GetProjectedColor(Texture2D clothTexture, float x, float y, Vector2 bodyStart, Vector2 bodyEnd, Color displaceColor, Color defaultColor)
        {
            //Convert to cloth x,y values
            var clothX = Mathf.FloorToInt(((x - bodyStart.x)/(bodyEnd.x - bodyStart.x)) * clothTexture.width);
            var clothY = Mathf.FloorToInt(((y - bodyStart.y)/(bodyEnd.y - bodyStart.y)) * clothTexture.height);

            //If the cloth texture pixel is transparent return displaceColor
            var isTransparent = clothTexture.GetPixel(clothX, clothY).a < 0.001f;
            return isTransparent ? defaultColor : displaceColor; 
        }

        public bool IsBetween(float x, float y, Vector2 start, Vector2 end)
        {
            //if x is not inside the x start and end
            if (x < start.x || x > end.x) return false;
            //if y is not inside the y start and end
            if (y < start.y || y > end.y) return false;

            return true;
        }

        public void UvToWorldspace(Vector2 uv, Mesh mesh, out Vector3 positionWs, out Vector3 normal)
        {
            positionWs = Vector3.zero;
            normal = Vector3.zero;

            var tris = mesh.triangles;
            var uvs = mesh.uv;
            var verts = mesh.vertices;
            var normals = mesh.normals;

            for (var i = 0; i < tris.Length; i += 3) {
                var u1 = uvs[tris[i]]; // get the triangle UVs
                var u2 = uvs[tris[i+1]];
                var u3 = uvs[tris[i+2]];
                // calculate triangle area - if zero, skip it
                var a = Area(u1, u2, u3); if (a == 0) continue;
                // calculate barycentric coordinates of u1, u2 and u3
                // if anyone is negative, point is outside the triangle: skip it
                var a1 = Area(u2, u3, uv)/a; if (a1 < 0) continue;
                var a2 = Area(u3, u1, uv)/a; if (a2 < 0) continue;
                var a3 = Area(u1, u2, uv)/a; if (a3 < 0) continue;
                // point inside the triangle - find mesh position by interpolation...
                var p3D = a1*verts[tris[i]]+a2*verts[tris[i+1]]+a3*verts[tris[i+2]];
                //Compute the average normal of the 3 triangle verts
                normal = (normals[tris[i]] + normals[tris[i+1]] + normals[tris[i+2]])/3;
                // and return it in world coordinates:
                positionWs = transform.TransformPoint(p3D);
                return;
            }
        }

        // calculate signed triangle area using a kind of "2D cross product":
        public float Area(Vector2 p1, Vector2 p2, Vector2 p3) {
            var v1 = p1 - p3;
            var v2 = p2 - p3;
            return (v1.x * v2.y - v1.y * v2.x)/2;
        }


        /// <summary>
        /// Get the main body mesh renderer for a character
        /// </summary>
        public SkinnedMeshRenderer GetBodyMeshRenderer()
        {
            var bodyMeshRenderers = GetMeshRenderers(ChaControl.objBody, findAll: true);
            var body = bodyMeshRenderers.FindAll(x => x?.name == BodyMeshName);
            if (body == null || body.Count <= 0)
            {
                return null;
            }

            return body[0];
        }


        /// <summary>
        /// Will get any Mesh Renderers for the given ChaControl.objxxx passed in
        /// </summary>
        /// <param name="chaControlObjs">The ChaControl.objxxx to fetch mesh renderers from  Might work for other GameObjects as well</param>
        internal static List<SkinnedMeshRenderer> GetMeshRenderers(GameObject[] chaControlObjs, bool findAll = false) 
        {            
            var renderers = new List<SkinnedMeshRenderer>();
            if (chaControlObjs == null) return renderers;

            foreach(var chaControlObj in chaControlObjs) 
            {
                if (chaControlObj == null) continue;

                var skinnedItems = GetMeshRenderers(chaControlObj, findAll);
                if (skinnedItems != null && skinnedItems.Count > 0) 
                {
                    renderers.AddRange(skinnedItems);
                }
            }

            return renderers;
        }
        

        internal static List<SkinnedMeshRenderer> GetMeshRenderers(GameObject characterObj, bool findAll = false) 
        {            
            var renderers = new List<SkinnedMeshRenderer>();
            if (characterObj == null) return renderers;

            var skinnedItem = characterObj.GetComponentsInChildren<SkinnedMeshRenderer>(findAll);            
            if (skinnedItem.Length > 0) 
            {
                renderers.AddRange(skinnedItem);
            }

            return renderers;
        }


        /// <summary>
        /// Raycast from the clothing vert to the direction passed and get the distance if it hits the mesh collider
        /// </summary>
        public RaycastHit RayCastToMeshCollider(Vector3 origin, float maxDistance, Vector3 direction, MeshCollider meshCollider)
        {
            var ray = new Ray(origin, direction);

            //Ray cast to the mesh collider
            meshCollider.Raycast(ray, out var hit, maxDistance);

            //Will return maxDistance if nothing is hit
            return hit;
        }


        /// <summary>
        /// Create a new mesh collider on a skinned mesh renderer, if one already exists, skip this step
        /// </summary>
        public void CreateMeshCollider(SkinnedMeshRenderer bodySmr = null)
        {        
            var colliderExists = GetMeshCollider(bodySmr);
            if (colliderExists != null) return;

            //Create the collider component
            var collider = bodySmr.transform.gameObject.AddComponent<MeshCollider>();

            var bakedMesh = new Mesh();
            bodySmr.BakeMesh(bakedMesh);
            //Copy the current base body mesh to use as the collider
            var meshCopy = (Mesh)UnityEngine.Object.Instantiate(bodySmr.sharedMesh);
            
            bakedMesh.vertices = OffSetMeshCollider(bodySmr, bakedMesh.vertices);

            //Create mesh instance
            bodySmr.sharedMesh = bodySmr.sharedMesh;
            collider.sharedMesh = bakedMesh;            
        }


        /// <summary>
        /// In order to line up the mesh collider with the baked cloth meshes, convert it to localspace
        /// </summary>
        public Vector3[] OffSetMeshCollider(SkinnedMeshRenderer bodySmr, Vector3[] originalVerts)
        {
            var shiftedVerts = new Vector3[originalVerts.Length];

            //Convert the verts back into locaalspace
            //  Otherwise the raycast wont pass through the collider mesh
            for (int i = 0; i < originalVerts.Length; i++)
            {                
                shiftedVerts[i] = bodySmr.transform.InverseTransformPoint(originalVerts[i]);
            } 


            return shiftedVerts;
        }


        /// <summary>
        /// Get an existing body mesh collider
        /// </summary>
        public MeshCollider GetMeshCollider(SkinnedMeshRenderer bodySmr = null)
        {
            if (bodySmr == null) bodySmr = GetBodyMeshRenderer();
            if (bodySmr == null) return null;

            //Get the collider component if it exists
            var collider = bodySmr.gameObject.GetComponent<MeshCollider>();
            if (collider == null) return null;

            return collider;
        }


        /// <summary>
        /// Destroy an existing mesh collider
        /// </summary>
        public void RemoveMeshCollider()
        {
            var collider = GetMeshCollider();
            if (collider != null) Destroy(collider);
        }



        internal void SaveTexture(Texture2D texture)
        {
            #if KK && !KKS
                //then Save To Disk as PNG
                byte[] bytes = texture.EncodeToPNG();
                var dirPath = Application.dataPath + "/../";
                // if(!Directory.Exists(dirPath)) {
                //     Directory.CreateDirectory(dirPath);
                // }
                File.WriteAllBytes(dirPath + "textureResult.png", bytes);
            #endif
        }
    }
}