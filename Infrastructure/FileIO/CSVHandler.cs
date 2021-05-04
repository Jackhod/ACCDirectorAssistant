using ACCAssistedDirector.Core.Services.Interfaces;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Infrastructure.FileIO {
    public class CSVHandler<T> : ICSVHelperService<T> {
        private FileStream stream;
        private StreamWriter writer;
        private StreamReader reader;

        public void WriteToFile(string path, bool writeHeader, List<T> records) {
            try {

                stream = File.Open(path, FileMode.Create);
                Write(writeHeader, records);

            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
            }
        }

        public void AppendToFile(string path, List<T> records) {
            try {

                stream = File.Open(path, FileMode.Append);
                Write(false, records);

            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
            }
        }

        private void Write(bool writeHeader, List<T> records) {

            using (writer = new StreamWriter(stream))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture)) {

                if (writeHeader) {
                    csvWriter.WriteHeader<T>();
                    csvWriter.NextRecord();
                }

                foreach (var record in records) {
                    csvWriter.WriteRecord(record);
                    csvWriter.NextRecord();
                }
            }

            //writer.Flush();
            stream.Close();
        }
       
        public IEnumerable<T> ReadFromFile(string path) {
            try {

                reader = new StreamReader(path);
                var csvReader = new CsvReader(reader, CultureInfo.CurrentCulture);
                return csvReader.GetRecords<T>();

            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
                return null;
            }            
        }

        public void CloseWriter() {
            try {

                if (writer != null) {
                    writer.Dispose();
                    writer.Flush();
                    stream.Close();
                }

            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
            }
        }

        public void CloseReader() {
            try {
                if (reader != null) {
                    reader.Dispose();
                    reader.Close();
                }

            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.StackTrace);
            }
        }
    }
}
