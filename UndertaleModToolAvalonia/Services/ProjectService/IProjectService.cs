using System;
using UndertaleModLib.Project;

namespace UndertaleModToolAvalonia.Services.ProjectService
{
    public interface IProjectService
    {
        public ProjectContext? Project { get; }

        public event EventHandler? ProjectChanged;

        public void AssignNewProject(ProjectContext project);

        public void UnloadProject();
    }
}
