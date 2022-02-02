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
            if (TestPlugin.TriggerTextureCompute.Value.IsDown())
            {
                TriggerMaterialPaint();
            }
        }






        internal void TriggerMaterialPaint() 
        {
            //White goes out, black in, and grey no change
            var defaultColor = Color.grey;
            var displaceColor = Color.white;
            
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

                //Raycast each cloth vertex towards meshcollider and get uv hit
                for (int i = 0; i < verts.Length; i++)
                {
                    var _direction = -normals[i];
                    var origin = verts[i];
                    var tryHit = RayCastToMeshCollider(origin, rayMaxDistance, _direction, meshCollider);

                    //If we hit the body
                    if (tryHit.distance > 0 && tryHit.distance < rayMaxDistance) 
                    {           
                        hitCount++;
                        //Convert UV coords to pixel coords
                        var x = Mathf.FloorToInt(bakedMesh.uv[i].x * clothTexture.width);
                        var y = Mathf.FloorToInt(bakedMesh.uv[i].y * clothTexture.height);

                        //Check the cloth vert color
                        var color = clothTexture.GetPixel(x, y);
                        var isTransparent = color.a < 0.01f;                        
                        if (!isTransparent) continue;

                        //If the uv hit, then we have a triangle index.  Get the vert indexes from that
                        var i1 = bodyTriangles[tryHit.triangleIndex * 3 + 0];
                        var i2 = bodyTriangles[tryHit.triangleIndex * 3 + 1];
                        var i3 = bodyTriangles[tryHit.triangleIndex * 3 + 2];
                        
                        bodyIsVisibleList[i1] = true;
                        bodyIsVisibleList[i2] = true;
                        bodyIsVisibleList[i3] = true;
                        hitUvCoordList[i1] = tryHit.textureCoord;
                        hitUvCoordList[i2] = tryHit.textureCoord;
                        hitUvCoordList[i3] = tryHit.textureCoord;
                    }        
                }    
                TestPlugin.Logger.LogInfo($" {smr.name} hitCount {hitCount}");
                if (hitCount <= 0) continue;


                //For each body Uv, if it has transparent cloth above, make its pixel displaceOutColor
                for (int i = 0; i < bodyIsVisibleList.Length; i++)
                {              
                    if (!bodyIsVisibleList[i]) continue;
                                 
                    //Convert UV coords to pixel coords
                    var x = Mathf.FloorToInt(hitUvCoordList[i].x * bodyDisplacementText.width);
                    var y = Mathf.FloorToInt(hitUvCoordList[i].y * bodyDisplacementText.height);

                    // TestPlugin.Logger.LogWarning($" setting displaceColor i:{i} {x},{y} to {displaceColor}");
                    bodyDisplacementText.SetPixel(x, y, displaceColor);                                        
                }                
                bodyDisplacementText.Apply();
                

                //Now do it again for all other cloth meshes
            }      

            //Save the texture as a png to the rootGame directory for debugging
            // SaveTexture(bodyDisplacementText);
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