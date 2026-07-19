using UnityEngine;
using UnityEngine.Rendering;

namespace TiberiumDusk.Client
{
    /// <summary>
    /// Runtime materials that work under URP when it is active and fall back to
    /// the built-in pipeline otherwise — the project stays openable even before
    /// the URP setup step has been run.
    /// </summary>
    public static class MaterialFactory
    {
        private static Shader _shader;

        private static Shader PickShader()
        {
            if (_shader != null) return _shader;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                _shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (_shader == null) _shader = Shader.Find("Standard");
            if (_shader == null) _shader = Shader.Find("Diffuse");
            return _shader;
        }

        public static Material Solid(Color color)
        {
            var material = new Material(PickShader());
            // URP/Lit uses _BaseColor; Standard uses _Color. Set both.
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            // Matte look for the cold, desaturated world.
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.1f);
            return material;
        }
    }
}
