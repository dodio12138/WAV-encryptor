using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Media;
using System.Runtime.InteropServices;

namespace PlayEverythingLikeMusic
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void openMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "音频文件 (*.mp3;*.wav)|*.mp3;*.wav|视频文件 (*.mp4)|*.mp4|所有文件 (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                mediaPlayer.Source = new Uri(openFileDialog.FileName);
                mediaPlayer.Play();
            }
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Play();
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
        }

        private void forwardButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Position += TimeSpan.FromSeconds(5);
        }

        private void backwardButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Position -= TimeSpan.FromSeconds(5);
        }

        private void progressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan ts = TimeSpan.FromSeconds(progressSlider.Value);
            mediaPlayer.Position = ts;
        }

        private void mediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            // 获取媒体总时长
            TimeSpan duration = mediaPlayer.NaturalDuration.TimeSpan;

            // 更新进度滑块的最大值和媒体总时长
            progressSlider.Maximum = duration.TotalSeconds;
            //totalTimeLabel.Content = duration.ToString(@"hh\:mm\:ss");

            // 使用 DispatcherTimer 定时器更新进度滑块的值
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += (o, ev) =>
            {
                if (mediaPlayer.Source != null)
                {
                    // 更新进度滑块的值为当前媒体播放位置
                    progressSlider.Value = mediaPlayer.Position.TotalSeconds;
                    // 更新当前时间标签的值为当前媒体播放位置
                    //currentTimeLabel.Content = mediaPlayer.Position.ToString(@"hh\:mm\:ss");
                }
                else
                {
                    timer.Stop();
                }
            };
            timer.Start();
        }

        private void mediaPlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mediaPlayer.Pause();
        }

        private void mediaPlayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mediaPlayer.Play();
        }

        private void mediaPlayer_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (IsMediaFile(file))
                    {
                        e.Effects = DragDropEffects.Copy;
                        return;
                    }
                }
            }
            e.Effects = DragDropEffects.None;
        }

        private void mediaPlayer_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (IsMediaFile(file))
                    {
                        //mediaPlayer.Source = new Uri(file);
                        File.WriteAllBytes("output.wav", Everything2wav(file).ToArray());
                        mediaPlayer.Source = new Uri("output.wav");
                        mediaPlayer.Play();
                        return;
                    }
                }
            }
        }

        private bool IsMediaFile(string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName).ToLower();
            return extension == ".mp3" || extension == ".wav" || extension == ".mp4" || extension == ".pdf";
        }

        private MemoryStream Everything2wav(string fileName)
        {
            byte[] fileData = File.ReadAllBytes(fileName);

            MemoryStream memoryStream;

            uint wavDataSize = (uint)fileData.Length;
            uint fileDataSize = 36 + wavDataSize;

            WAV wavHeader = new WAV();

            // 填充 WAV 头部信息
            wavHeader.wav_rief.RiffHeader = 0x46464952;    // "RIFF"
            wavHeader.wav_rief.FileSize = fileDataSize;
            wavHeader.wav_rief.WaveHeader = 0x45564157;    // "WAVE"
            wavHeader.wav_fmt.FmtHeader = 0x20746D66;     // "fmt "
            wavHeader.wav_fmt.FmtSize = 16;
            wavHeader.wav_fmt.AudioFormat = 1;
            wavHeader.wav_fmt.NumChannels = 1;
            wavHeader.wav_fmt.SampleRate = 44100;
            wavHeader.wav_fmt.ByteRate = 44100 * 2;
            wavHeader.wav_fmt.BlockAlign = 2;
            wavHeader.wav_fmt.BitsPerSample = 16;
            wavHeader.wav_data.DataHeader = 0x61746164;    // "data"
            wavHeader.wav_data.DataSize = wavDataSize;

            // 将 WAV 头部信息转换为二进制数据
            byte[] wavHeaderData;
            int wavHeaderSize = Marshal.SizeOf(wavHeader);
            wavHeaderData = new byte[wavHeaderSize];
            IntPtr wavHeaderPtr = Marshal.AllocHGlobal(wavHeaderSize);
            Marshal.StructureToPtr(wavHeader, wavHeaderPtr, false);
            Marshal.Copy(wavHeaderPtr, wavHeaderData, 0, wavHeaderSize);
            Marshal.FreeHGlobal(wavHeaderPtr);

            using (memoryStream = new MemoryStream(fileData))
            {
                long currentPosition = memoryStream.Position;
                memoryStream.Write(wavHeaderData, 0, wavHeaderData.Length);
                memoryStream.Seek(0, SeekOrigin.End);
                memoryStream.Write(fileData, 0, fileData.Length);
                memoryStream.Seek(currentPosition, SeekOrigin.Begin);
            }    
            return memoryStream;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WAV
    {
        public WAV_RIEF wav_rief;
        public WAV_FMT wav_fmt;
        public WAV_DATA wav_data;
    }

    public struct WAV_RIEF
    {
        // RIFF Chunk
        public uint RiffHeader;         // "RIFF"
        public uint FileSize;           // 文件长度
        public uint WaveHeader;         // "WAVE"
    }

    public struct WAV_FMT
    {
        // fmt Chunk
        public uint FmtHeader;          // "fmt "
        public uint FmtSize;            // 格式数据长度
        public ushort AudioFormat;      // 编码格式：1-PCM，3-IEEE Float，6-ALaw，7-uLaw
        public ushort NumChannels;      // 声道数
        public uint SampleRate;         // 采样率
        public uint ByteRate;           // 每秒字节数
        public ushort BlockAlign;       // 块对齐字节数
        public ushort BitsPerSample;    // 量化位数
    }

    public struct WAV_DATA
    {
        // data Chunk
        public uint DataHeader;         // "data"
        public uint DataSize;           // 数据长度
    }
}

