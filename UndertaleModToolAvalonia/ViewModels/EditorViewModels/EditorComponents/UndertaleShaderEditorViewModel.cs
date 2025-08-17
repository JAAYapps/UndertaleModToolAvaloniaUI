using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleShaderEditorViewModel : EditorContentViewModel
    {
        [ObservableProperty]
        private UndertaleShader shader;

        // Expose shader source strings as properties
        public string GLSL_ES_Vertex
        {
            get => Shader.GLSL_ES_Vertex?.Content ?? "";
            set => Shader.GLSL_ES_Vertex.Content = value;
        }

        public string GLSL_ES_Fragment
        {
            get => Shader.GLSL_ES_Fragment?.Content ?? "";
            set => Shader.GLSL_ES_Fragment.Content = value;
        }

        public string GLSL_Vertex
        {
            get => Shader.GLSL_Vertex?.Content ?? "";
            set => Shader.GLSL_Vertex.Content = value;
        }

        public string GLSL_Fragment
        {
            get => Shader.GLSL_Fragment?.Content ?? "";
            set => Shader.GLSL_Fragment.Content = value;
        }

        public string HLSL9_Vertex
        {
            get => Shader.HLSL9_Vertex?.Content ?? "";
            set => Shader.HLSL9_Vertex.Content = value;
        }

        public string HLSL9_Fragment
        {
            get => Shader.HLSL9_Fragment?.Content ?? "";
            set => Shader.HLSL9_Fragment.Content = value;
        }

        public IEnumerable<UndertaleShader.ShaderType> ShaderTypes { get; } =
            System.Enum.GetValues(typeof(UndertaleShader.ShaderType)).Cast<UndertaleShader.ShaderType>();

        public UndertaleShaderEditorViewModel(string title, UndertaleShader shader) : base(title)
        {
            Shader = shader;
        }
    }
}
