// Copyright (c) Mixed Reality Toolkit Contributors
// Licensed under the BSD 3-Clause

// Disable "missing XML comment" warning for samples. While nice to have, this XML documentation is not required for samples.
#pragma warning disable CS1591

using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using MixedReality.Toolkit.Input;
using TMPro;

namespace MixedReality.Toolkit.Examples.Demos
{
    /// <summary>
    /// Basic example of how to use interactors to create a simple whiteboard-like drawing system.
    /// Uses MRTKBaseInteractable, but not StatefulInteractable.
    /// </summary>
    [AddComponentMenu("MRTK/Examples/WhiteboardMod")]
    internal class WhiteboardMod : MRTKBaseInteractable
    {
        // Preferably power of two!
        public int TextureSize;

        // Color used to draw on texture.
        public Color32 drawingColor = new Color32(0, 0, 0, 255);

        // The internal texture reference we will modify.
        // Bound to the renderer on this GameObject. <private>
        private Texture2D texture;

        public Texture2D originalTexture;

        // Used draw a full line between current frame + last frame's "paintbrush" position.
        private Dictionary<IXRInteractor, Vector2> lastPositions = new Dictionary<IXRInteractor, Vector2>();

        // Preferably an odd number!
        public int penThickness = 3;
        public TMP_Text percentText;
        public AudioSource audioSource;
        public AudioClip clip;
        [Range(0.0f, 1.0f)]
        public float amplitude;
        [Range(0.0f, 1.0f)]
        public float upperBound;
        [Range(0.0f, 1.0f)]
        public float lowerBound;
        private float pixelCount;
        private float curpixelCount;
        private int startSplat;
        private int endSplat;
        float percent;

        /// <summary>
        /// A Unity event function that is called on the frame when a script is enabled just before any of the update methods are called the first time.
        /// </summary> 
        private void Start()
        {
            // Create new texture and bind it to renderer/material.
            
            texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            //
            texture.SetPixels(originalTexture.GetPixels());
            texture.Apply();
            //
            texture.hideFlags = HideFlags.HideAndDontSave;
            Renderer rend = GetComponent<Renderer>();
            rend.material.SetTexture("_MainTex", texture);
            pixelCount = TextureSize * TextureSize;
            curpixelCount = pixelCount;
            startSplat = - (int)(penThickness / 2);
            endSplat = -startSplat + 1;
        }

        public void ClearDrawing()
        {
            // Destroys texture and re-inits.
            Object.Destroy(texture);
            Start();
        }

        public void ChangeColorYellow()
        {
            drawingColor = new Color(1.0f, 0.7f, 0.0f, 1.0f);
        }

        public void ChangeColorGreen()
        {
            drawingColor = new Color(0.0f, 1.0f, 0.7f, 1.0f);
        }
        public void ChangeColorRed()
        {
            drawingColor = new Color(1.0f, 0.0f, 0.2f, 1.0f);
        }

        /// <summary>
        /// A Unity event function that is called when the script component has been destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            Object.Destroy(texture);
            base.OnDestroy();
        }

        void Update()
        {
            if (percent > lowerBound && percent < upperBound && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(clip, amplitude);
                // Debug.Log("audio is playing");
            }
            else if(percent >= upperBound && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            else if(percent >= upperBound && !audioSource.isPlaying)
            {
                percentText.SetText("Task Complete");
                percentText.color = new Color32(37, 148, 65, 255);
            }
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {   
            // Dynamic is effectively just your normal Update().
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                NativeArray<Color32> data = texture.GetRawTextureData<Color32>();

                foreach (var interactor in interactorsSelecting)
                {
                    // attachTransform will be the actual point of the touch interaction (e.g. index tip)
                    // Most applications will probably just end up using this local touch position.
                    Vector3 localTouchPosition = transform.InverseTransformPoint(interactor.GetAttachTransform(this).position);

                    // For whiteboard drawing: compute UV coordinates on texture by flattening Vector3 against the plane and adding 0.5f.
                    Vector2 uvTouchPosition = new Vector2(localTouchPosition.x + 0.5f, localTouchPosition.y + 0.5f);

                    // Compute pixel coords as a fraction of the texture dimension
                    Vector2 pixelCoordinate = Vector2.Scale(new Vector2(TextureSize, TextureSize), uvTouchPosition);

                    // Have we seen this interactor before? If not, last position = current position.
                    if (!lastPositions.TryGetValue(interactor, out Vector2 lastPosition))
                    {
                        lastPosition = pixelCoordinate;
                    }

                    // Very simple "line drawing algorithm".
                    for (int i = 0; i < Vector2.Distance(pixelCoordinate, lastPosition); i++)
                    {
                        DrawSplat(Vector2.Lerp(lastPosition, pixelCoordinate, i / Vector2.Distance(pixelCoordinate, lastPosition)), data);
                        curpixelCount -= penThickness;
                        if (curpixelCount > 0) {
                            percent = 1 - (curpixelCount / pixelCount);
                            percentText.SetText("Drawing Progress: "+ (percent * 100).ToString("n2") + "%");
                        }
                        else
                        {
                            percent = 1.0f;
                        }
                    }
                    
                    // Write/update the last-position.
                    if (lastPositions.ContainsKey(interactor))
                    {
                        lastPositions[interactor] = pixelCoordinate;
                    }
                    else
                    {
                        lastPositions.Add(interactor, pixelCoordinate);
                    }
                }

                texture.Apply(false);
            }
        }

        /// <inheritdoc />
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            // Remove the interactor from our last-position collection when it leaves.
            lastPositions.Remove(args.interactorObject);
        }

        // Draws a 3x3 splat onto the texture at the specified pixel coordinates.
        private void DrawSplat(Vector2 pixelCoordinate, NativeArray<Color32> data)
        {
            // Compute index of pixel in NativeArray.
            int pixelIndex = Mathf.RoundToInt(pixelCoordinate.x) + TextureSize * Mathf.RoundToInt(pixelCoordinate.y);

            // Draw a penThickness x penThickness splat, centered on pixelIndex.
            for (int y = startSplat; y < endSplat; y++)
            {
                for (int x = startSplat; x < endSplat; x++)
                {
                    data[Mathf.Clamp(pixelIndex + x + (TextureSize * y), 0, data.Length - 1)] = drawingColor;
                }
            }
        }
    }
}
#pragma warning restore CS1591
