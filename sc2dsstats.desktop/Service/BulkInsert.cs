using Microsoft.Extensions.Hosting;
using sc2dsstats.decode.Models;
using sc2dsstats.decode.Service;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats.desktop.Service
{
    public class BulkInsert : BackgroundService
    {
        private const char CR = '\r';
        private const char LF = '\n';
        private const char NULL = (char)0;

        private DSReplayContext _context;
        private DSoptions _options;
        public event EventHandler ReplayProcessed;

        public BulkInsert(DSReplayContext context, DSoptions options)
        {
            _context = context;
            _options = options;
        }

        protected virtual void OnReplayProcessed(BulkInsertArgs e)
        {
            EventHandler handler = ReplayProcessed;
            handler?.Invoke(this, e);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _options.Decoding = true;
            BulkInsertArgs arg = new BulkInsertArgs();
            await Task.Run(() =>
            {
                DateTime t = DateTime.UtcNow;
                
                var jsonfile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\sc2dsstats_web\\data.json";

                if (!File.Exists(jsonfile))
                    return;

                using (FileStream fs = File.OpenRead(jsonfile))
                {
                    arg.Total = CountLinesMaybe(fs);
                }

                using (var md5 = MD5.Create())
                {
                    foreach (var line in File.ReadAllLines(jsonfile))
                    {
                        dsreplay rep = JsonSerializer.Deserialize<dsreplay>(line);
                        if (rep != null)
                        {
                            rep.Init();
                            rep.GenHash();

                            string reppath = Status.ReplayFolder.Where(x => x.Value == rep.REPLAY.Substring(0, 47)).FirstOrDefault().Key;
                            reppath += "/" + rep.REPLAY.Substring(48);
                            reppath += ".SC2Replay";
                            rep.REPLAY = reppath;
                            DSReplay Rep = Map.Rep(rep);
                            
                            
                            DBService.SaveReplay(_context, Rep, true);
                            
                            arg.Count++;
                            OnReplayProcessed(arg);

                        }
                    }
                }
                _context.SaveChanges();
                Console.WriteLine((DateTime.UtcNow - t).TotalSeconds);
            });
            arg.Done = true;
            OnReplayProcessed(arg);
            _options.Decoding = false;
        }

        public static long CountLinesMaybe(Stream stream)
        {
            var lineCount = 0L;

            var byteBuffer = new byte[1024 * 1024];
            const int BytesAtTheTime = 4;
            var detectedEOL = NULL;
            var currentChar = NULL;

            int bytesRead;
            while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
            {
                var i = 0;
                for (; i <= bytesRead - BytesAtTheTime; i += BytesAtTheTime)
                {
                    currentChar = (char)byteBuffer[i];

                    if (detectedEOL != NULL)
                    {
                        if (currentChar == detectedEOL) { lineCount++; }

                        currentChar = (char)byteBuffer[i + 1];
                        if (currentChar == detectedEOL) { lineCount++; }

                        currentChar = (char)byteBuffer[i + 2];
                        if (currentChar == detectedEOL) { lineCount++; }

                        currentChar = (char)byteBuffer[i + 3];
                        if (currentChar == detectedEOL) { lineCount++; }
                    }
                    else
                    {
                        if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                        i -= BytesAtTheTime - 1;
                    }
                }

                for (; i < bytesRead; i++)
                {
                    currentChar = (char)byteBuffer[i];

                    if (detectedEOL != NULL)
                    {
                        if (currentChar == detectedEOL) { lineCount++; }
                    }
                    else
                    {
                        if (currentChar == LF || currentChar == CR)
                        {
                            detectedEOL = currentChar;
                            lineCount++;
                        }
                    }
                }
            }

            if (currentChar != LF && currentChar != CR && currentChar != NULL)
            {
                lineCount++;
            }
            return lineCount;
        }
    }

    public class BulkInsertArgs : EventArgs
    {
        public long Count { get; set; } = 0;
        public long Total { get; set; } = 0;
        public bool Done { get; set; } = false;
    }


}
