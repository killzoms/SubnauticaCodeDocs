using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AssemblyCSharp.WorldStreaming
{
    public sealed class ClipmapChunk : MonoBehaviour, IVoxelandChunk2
    {
        private const float fadeInDuration = 1f;

        [NonSerialized]
        public readonly List<MeshFilter> hiFilters = new List<MeshFilter>();

        [NonSerialized]
        public readonly List<MeshRenderer> hiRenders = new List<MeshRenderer>();

        [NonSerialized]
        public readonly List<MeshFilter> grassFilters = new List<MeshFilter>();

        [NonSerialized]
        public readonly List<MeshRenderer> grassRenders = new List<MeshRenderer>();

        [NonSerialized]
        public MeshCollider collision;

        List<MeshFilter> IVoxelandChunk2.hiFilters => hiFilters;

        List<MeshRenderer> IVoxelandChunk2.hiRenders => hiRenders;

        List<MeshFilter> IVoxelandChunk2.grassFilters => grassFilters;

        List<MeshRenderer> IVoxelandChunk2.grassRenders => grassRenders;

        MeshCollider IVoxelandChunk2.collision
        {
            get
            {
                return collision;
            }
            set
            {
                collision = value;
            }
        }

        public void SetPosition(int downsamples, Int3 offset)
        {
            int num = 1 << downsamples;
            base.transform.localScale = new Vector3(num, num, num);
            base.transform.localPosition = (Vector3)offset;
        }

        public void SetRenderersEnabled(bool enabled, bool fade)
        {
            float fadeAmount = (fade ? 0f : 1f);
            for (int i = 0; i < hiRenders.Count; i++)
            {
                SetRendererEnabled(hiRenders[i], enabled, fadeAmount);
            }
            for (int j = 0; j < grassRenders.Count; j++)
            {
                SetRendererEnabled(grassRenders[j], enabled, fadeAmount);
            }
        }

        public void SetRenderersFadeAmount(float fadeAmount)
        {
            for (int i = 0; i < hiRenders.Count; i++)
            {
                hiRenders[i].SetFadeAmount(fadeAmount);
            }
            for (int j = 0; j < grassRenders.Count; j++)
            {
                hiRenders[j].SetFadeAmount(fadeAmount);
            }
        }

        private void SetRendererEnabled(Renderer renderer, bool enabled, float fadeAmount)
        {
            renderer.enabled = enabled;
            renderer.SetFadeAmount(fadeAmount);
        }

        private IEnumerator FadeInAsync()
        {
            float startTime = Time.time;
            float endTime = startTime + 1f;
            float time;
            do
            {
                yield return null;
                time = Time.time;
                float renderersFadeAmount = Mathf.InverseLerp(startTime, endTime, time);
                SetRenderersFadeAmount(renderersFadeAmount);
            }
            while (time < endTime);
        }

        public void FadeIn(bool fade)
        {
            SetRenderersEnabled(enabled: true, fade);
            if (fade)
            {
                StartCoroutine(FadeInAsync());
            }
        }

        [ContextMenu("Fade in")]
        public void FadeIn()
        {
            FadeIn(fade: true);
        }

        [ContextMenu("Show")]
        public void Show()
        {
            SetRenderersEnabled(enabled: true, fade: false);
        }

        [ContextMenu("Hide")]
        public void Hide()
        {
            SetRenderersEnabled(enabled: false, fade: false);
        }

        MeshCollider IVoxelandChunk2.EnsureCollision()
        {
            return VoxelandChunk.EnsureCollision(this);
        }

        [SpecialName]
        Transform IVoxelandChunk2.transform => transform;

        [SpecialName]
        GameObject IVoxelandChunk2.gameObject => gameObject;
    }
}
