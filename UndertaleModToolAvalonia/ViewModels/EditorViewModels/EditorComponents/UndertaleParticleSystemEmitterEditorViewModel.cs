using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Services.FileService;
using UndertaleModToolAvalonia.Services.TextureCacheService;

namespace UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents
{
    public partial class UndertaleParticleSystemEmitterEditorViewModel(string title, UndertaleParticleSystemEmitter undertaleParticleSystemEmitter, EditorViewModel editor, IFileService fileService, ITextureCacheService textureCacheService) : EditorContentViewModel(title)
    {
        public EditorViewModel Editor { get; } = editor;
        public ITextureCacheService TextureCacheService { get; } = textureCacheService;
        public IFileService FileService { get; } = fileService;

        [ObservableProperty]
        private UndertaleParticleSystemEmitter undertaleParticleSystemEmitter = undertaleParticleSystemEmitter;

        public UndertaleParticleSystemEmitter.EmitMode[] EmitModes { get; } = 
            Enum.GetValues<UndertaleParticleSystemEmitter.EmitMode>();

        public UndertaleParticleSystemEmitter.DistributionEnum[] Distributions { get; } = 
            Enum.GetValues<UndertaleParticleSystemEmitter.DistributionEnum>();

        public UndertaleParticleSystemEmitter.EmitterShape[] EmitterShapes { get; } = 
            Enum.GetValues<UndertaleParticleSystemEmitter.EmitterShape>();

        public UndertaleParticleSystemEmitter.TextureEnum[] Textures { get; } = 
            Enum.GetValues<UndertaleParticleSystemEmitter.TextureEnum>();

        public UndertaleParticleSystemEmitter.TimeUnitEnum[] TimeUnits { get; } = 
            Enum.GetValues<UndertaleParticleSystemEmitter.TimeUnitEnum>();
    }
}
