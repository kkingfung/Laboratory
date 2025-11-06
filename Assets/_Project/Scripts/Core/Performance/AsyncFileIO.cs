using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// Asynchronous file I/O operations to prevent main thread blocking.
    /// All file operations run on background threads and don't impact frame rate.
    /// Use these methods instead of synchronous File.Read/Write operations.
    /// </summary>
    public static class AsyncFileIO
    {
        #region Text Operations

        /// <summary>
        /// Asynchronously write text to a file
        /// </summary>
        public static async Task WriteTextAsync(string filePath, string content, bool append = false)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] data = Encoding.UTF8.GetBytes(content);

                FileMode mode = append ? FileMode.Append : FileMode.Create;

                using (var fs = new FileStream(filePath, mode, FileAccess.Write, FileShare.Read, 4096, useAsync: true))
                {
                    await fs.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncFileIO] Failed to write to {filePath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously append text to a file
        /// </summary>
        public static async Task AppendTextAsync(string filePath, string content)
        {
            await WriteTextAsync(filePath, content, append: true);
        }

        /// <summary>
        /// Asynchronously read text from a file
        /// </summary>
        public static async Task<string> ReadTextAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[AsyncFileIO] File not found: {filePath}");
                    return null;
                }

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    byte[] buffer = new byte[fs.Length];
                    await fs.ReadAsync(buffer, 0, buffer.Length);
                    return Encoding.UTF8.GetString(buffer);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncFileIO] Failed to read from {filePath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously append a line to a log file (optimized for logging)
        /// </summary>
        public static async Task AppendLogLineAsync(string logFilePath, string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            await AppendTextAsync(logFilePath, logEntry);
        }

        #endregion

        #region Binary Operations

        /// <summary>
        /// Asynchronously write binary data to a file
        /// </summary>
        public static async Task WriteBytesAsync(string filePath, byte[] data)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await fs.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncFileIO] Failed to write bytes to {filePath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously read binary data from a file
        /// </summary>
        public static async Task<byte[]> ReadBytesAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[AsyncFileIO] File not found: {filePath}");
                    return null;
                }

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    byte[] buffer = new byte[fs.Length];
                    await fs.ReadAsync(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncFileIO] Failed to read bytes from {filePath}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region File Operations

        /// <summary>
        /// Asynchronously check if file exists (uses Task.Run for I/O)
        /// </summary>
        public static async Task<bool> FileExistsAsync(string filePath)
        {
            return await Task.Run(() => File.Exists(filePath));
        }

        /// <summary>
        /// Asynchronously delete a file
        /// </summary>
        public static async Task DeleteFileAsync(string filePath)
        {
            await Task.Run(() =>
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });
        }

        /// <summary>
        /// Asynchronously copy a file
        /// </summary>
        public static async Task CopyFileAsync(string sourcePath, string destPath, bool overwrite = true)
        {
            try
            {
                byte[] data = await ReadBytesAsync(sourcePath);
                if (data != null)
                {
                    await WriteBytesAsync(destPath, data);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncFileIO] Failed to copy {sourcePath} to {destPath}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Directory Operations

        /// <summary>
        /// Asynchronously ensure directory exists
        /// </summary>
        public static async Task EnsureDirectoryExistsAsync(string directoryPath)
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            });
        }

        /// <summary>
        /// Asynchronously get all files in directory
        /// </summary>
        public static async Task<string[]> GetFilesAsync(string directoryPath, string searchPattern = "*")
        {
            return await Task.Run(() =>
            {
                if (Directory.Exists(directoryPath))
                {
                    return Directory.GetFiles(directoryPath, searchPattern);
                }
                return new string[0];
            });
        }

        #endregion

        #region Serialization Helpers

        /// <summary>
        /// Asynchronously serialize object to JSON and save
        /// </summary>
        public static async Task SaveJsonAsync<T>(string filePath, T obj, bool prettyPrint = false)
        {
            string json = JsonUtility.ToJson(obj, prettyPrint);
            await WriteTextAsync(filePath, json);
        }

        /// <summary>
        /// Asynchronously load and deserialize JSON object
        /// </summary>
        public static async Task<T> LoadJsonAsync<T>(string filePath) where T : class
        {
            string json = await ReadTextAsync(filePath);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncFileIO] Failed to deserialize JSON from {filePath}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Batched Operations

        /// <summary>
        /// Asynchronously write multiple log entries (batched for efficiency)
        /// </summary>
        public static async Task BatchWriteLogsAsync(string logFilePath, string[] messages)
        {
            var sb = new StringBuilder();
            foreach (var message in messages)
            {
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            }

            await AppendTextAsync(logFilePath, sb.ToString());
        }

        #endregion
    }

    /// <summary>
    /// Buffered log writer for high-frequency logging without blocking
    /// </summary>
    public class BufferedLogWriter : IDisposable
    {
        private readonly string _logFilePath;
        private readonly StringBuilder _buffer;
        private readonly int _flushThreshold;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public BufferedLogWriter(string logFilePath, int flushThreshold = 10)
        {
            _logFilePath = logFilePath;
            _flushThreshold = flushThreshold;
            _buffer = new StringBuilder(1024);
        }

        /// <summary>
        /// Add a log entry to the buffer (thread-safe)
        /// </summary>
        public void WriteLine(string message)
        {
            if (_disposed) return;

            lock (_lock)
            {
                _buffer.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");

                // Auto-flush if threshold reached
                if (_buffer.Length > _flushThreshold * 100)
                {
                    _ = FlushAsync();
                }
            }
        }

        /// <summary>
        /// Flush buffer to file asynchronously
        /// </summary>
        public async Task FlushAsync()
        {
            if (_disposed) return;

            string content;
            lock (_lock)
            {
                if (_buffer.Length == 0) return;

                content = _buffer.ToString();
                _buffer.Clear();
            }

            await AsyncFileIO.AppendTextAsync(_logFilePath, content);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            // Flush remaining buffer
            _ = FlushAsync();
        }
    }
}
