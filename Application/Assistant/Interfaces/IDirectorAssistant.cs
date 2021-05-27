using Application.Services.Interfaces;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.Assistant.Interfaces {

    public delegate void NewTipsGeneratedDelegate(List<DirectorTipModel> tips);

    public interface IDirectorAssistant : Service {

        public List<DirectorTipModel> DirectorTips { get; set; }
        public bool IsAutoPilotActive { get; set; }
        public DirectorAssistantMLManager DirectorAssistantMLManager { get; set; }

        public event NewTipsGeneratedDelegate OnNewTipsGenerated;
    }
}
