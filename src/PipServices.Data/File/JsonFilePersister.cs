using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PipServices.Commons.Config;
using PipServices.Commons.Errors;
using PipServices.Data.Interfaces;

namespace PipServices.Data.File
{
    public sealed class JsonFilePersister<T> : ILoader<T>, ISaver<T>, IConfigurable
    {
        public string Path { get; set; }

        public JsonFilePersister()
            : this(null)
        {
        }

        public JsonFilePersister(string path)
        {
            Path = path;
        }

        public async Task<IEnumerable<T>> LoadAsync(string correlationId)
        {
            if (!System.IO.File.Exists(Path))
            {
                return new List<T>();
            }

            try
            {
                using (var reader = new StreamReader(System.IO.File.OpenRead(Path)))
                {
                    var json = await reader.ReadToEndAsync();

                    var list = JsonConvert.DeserializeObject<T[]>(json);

                    return list != null ? new List<T>(list) : new List<T>();
                }
            }
            catch (Exception ex)
            {
                throw new FileException(correlationId, "READ_FAILED", "Failed to read data file: " + Path, ex)
                    .WithCause(ex.Message);
            }
        }

        public async Task SaveAsync(string correlationId, IEnumerable<T> items)
        {
            try
            {
                using (var writer = new StreamWriter(System.IO.File.OpenWrite(Path)))
                {
                    var json = JsonConvert.SerializeObject(items.ToArray(), Formatting.Indented);

                    await writer.WriteAsync(json);
                }
            }
            catch (Exception ex)
            {
                throw new FileException(correlationId, "WRITE_FAILED", "Failed to write data file: " + Path, ex)
                    .WithCause(ex.Message);
            }
        }

        public void Configure(ConfigParams config)
        {
            if (config == null || !config.ContainsKey("path"))
                throw new ConfigException(null, "NO_PATH", "Data file path is not set");

            Path = config.GetAsString("path");
        }
    }
}
