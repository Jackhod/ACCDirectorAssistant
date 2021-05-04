using System;
using System.Collections.Generic;
using System.Text;

namespace ACCAssistedDirector.Core.Services.Interfaces {
    public interface ICSVHelperService<T>{

        public void WriteToFile(string path, bool writeHeader, List<T> records);
        public void AppendToFile(string path, List<T> records);
        public IEnumerable<T> ReadFromFile(string path);
        public void CloseWriter();
        public void CloseReader();
    }
}
