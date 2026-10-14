using System;
using UndertaleModLib.Project;

namespace UndertaleModToolAvalonia.Services.ProjectService
{
    public class ProjectService : IProjectService
    {
        public ProjectContext? Project { get; private set; }

        public event EventHandler? ProjectChanged;

        public void AssignNewProject(ProjectContext project)
        {
            Project = project;
            ProjectChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UnloadProject()
        {
            Project = null;
            ProjectChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
