using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Policy;
using System.Runtime.Remoting.Messaging;
using static System.Windows.Forms.AxHost;
using System.Data.SqlTypes;
using System.Security.Cryptography.X509Certificates;

namespace Visualization
{
    public partial class Form1 : Form
    {


        ushort[,] data; // массив кодов яркости пикселей
        ushort h, w; // создаем переменные высоту h и ширину w изображения

        Bitmap bitmap;
        private TrackBar[] trackBars; // Массив для TrackBar
        private TextBox[] textBoxes; // Массив для TextBox
        private byte minValue = 0; // Минимальное значение для всех TrackBar
        private byte maxValue = 255; // Максимальное значение для всех TrackBar

        public Form1()
        {
            InitializeComponent();
            InitializeControls();
            NamePicture.Text = "";
            ConfigureChart();

        }

        // Настройка графика
        private void ConfigureChart()
        {
            // Настройка оси X
            chart1.ChartAreas[0].AxisX.Minimum = 0; // Минимальное значение оси X
            chart1.ChartAreas[0].AxisX.Maximum = 1024; // Максимальное значение оси X
            chart1.ChartAreas[0].AxisX.Interval = 128; // Интервал между делениями (128 единиц)

            // Настройка оси Y
            chart1.ChartAreas[0].AxisY.Minimum = 0; // Минимальное значение оси Y
            chart1.ChartAreas[0].AxisY.Maximum = 256; // Максимальное значение оси Y
            chart1.ChartAreas[0].AxisY.Interval = 32; // Интервал между делениями оси Y (8 сегментов по оси Y)

            // Настройка делений (по 8 сегментов)
            chart1.ChartAreas[0].AxisX.MajorGrid.LineWidth = 1; // Толщина линий сетки оси X
            chart1.ChartAreas[0].AxisY.MajorGrid.LineWidth = 1; // Толщина линий сетки оси Y

            // Настройка подписи оси X
            chart1.ChartAreas[0].AxisX.LabelStyle.Format = "0"; // Форматирование меток оси X, например, целые числа

            // Настройка подписи оси Y
            chart1.ChartAreas[0].AxisY.LabelStyle.Format = "0"; // Форматирование меток оси Y
        }

        // Инициализация массивов для работы с trackbars и textboxs
        private void InitializeControls()
        {
            // Инициализация массивов
            trackBars = new TrackBar[] { trackBar1, trackBar2, trackBar3, trackBar4, trackBar5, trackBar6, trackBar7, trackBar8, trackBar9 };
            textBoxes = new TextBox[] { textBox1, textBox2, textBox3, textBox4, textBox5, textBox6, textBox7, textBox8, textBox9 };

            // Настройка диапазонов и подписка на события
            for (byte i = 0; i < trackBars.Length; i++)
            {
                trackBars[i].Minimum = minValue;
                trackBars[i].Maximum = maxValue;
                trackBars[i].Value = 0; // Устанавливаем начальное значение
                trackBars[i].Scroll += TrackBar_Scroll;

                textBoxes[i].Text = trackBars[i].Value.ToString();
                textBoxes[i].TextChanged += TextBox_TextChanged;
            }
        }

        // Функция обработки динамического изменения значения textbox в сооствествии с trackBar
        private void TrackBar_Scroll(object sender, EventArgs e)
        {
            // Определяем, какой TrackBar вызвал событие
            TrackBar trackBar = sender as TrackBar;
            int index = Array.IndexOf(trackBars, trackBar);

            if (index >= 0)
            {
                // Синхронизируем значение TextBox
                textBoxes[index].Text = trackBar.Value.ToString();
            }
        }

        // Функция обработки динамического изменения значения trackBar в сооствествии с textbox
        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            // Определяем, какой TextBox вызвал событие
            TextBox textBox = sender as TextBox;
            int index = Array.IndexOf(textBoxes, textBox);

            if (index >= 0 && int.TryParse(textBox.Text, out int value))
            {
                // Проверяем, чтобы значение было в пределах диапазона
                if (value >= trackBars[index].Minimum && value <= trackBars[index].Maximum)
                {
                    trackBars[index].Value = value; // Синхронизируем значение TrackBar
                }
            }
        }

        // Реализация кнопки загрзуки файла
        private void Loading_Click(object sender, EventArgs e)
        {
            // Создаем новый экземпляр OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Настройка диалогового окна
            openFileDialog.Title = "Выберите файл";
            openFileDialog.Filter = "Все файлы (*.*)|*.*"; // Вы можете изменить фильтр по своему усмотрению
            openFileDialog.InitialDirectory = @"C:\"; // Устанавливаем начальную директорию

            // Показываем диалоговое окно и проверяем, был ли выбран файл
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Получаем путь к выбранному файлу
                string filePath = openFileDialog.FileName;
                NamePicture.Text = Path.GetFileName(filePath);

                data = readFile(filePath); //функция для побайтового чтения файла

                bitmap = new Bitmap(w, h); //создание bitmap для заполнения пикселями (задали ширину и высоту)

                grafic(sender,e); // вызов функции обработки кодов яроксти пикселя
            }

        }

        // Реализация функции чтения файла
        private ushort[,] readFile(string filePath)  //реализация функции для побайтового чтения файла
        {

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                // Чтение первых двух байтов и запись в переменную h
                h = reader.ReadUInt16();  // Предполагается, что байты представляют короткое целое число

                // Чтение следующих двух байтов и запись в переменную w
                w = reader.ReadUInt16();  // Аналогично, читаем 2 байта

                data = new ushort[w, h];

                // Чтение данных в массив ushort
                for (ushort i = 0; i < w; i++)
                {
                    for (ushort j = 0; j < h; j++)
                    {
                        data[i, j] = reader.ReadUInt16();
                    }
                }
                return data;
            }
        }

        //  функция обработки кодов яроксти пикселя
            // преобразование кодов яроксти пикселя в яроксть пикселя путём приведения к диапазону 0-255
        private void grafic(object sender, EventArgs e)
        {
            // Очищаем график перед построением
            chart1.Series.Clear();

            // Создаем серию для графика
            var series = new System.Windows.Forms.DataVisualization.Charting.Series
            {
                ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line,
                Color = Color.Blue,
                BorderWidth = 2,
                IsVisibleInLegend = false,
            };
            chart1.Series.Add(series);

            // Установим ось X и Y
            chart1.ChartAreas[0].AxisX.Title = "Код Яркости пикселя";
            chart1.ChartAreas[0].AxisY.Title = "Яркость пикселя";

            // Построение графика
            for (byte i = 0; i < trackBars.Length - 1; i++)
            {
                ushort a = (ushort)(i * 128);        // Начало диапазона X
                ushort b = (ushort)((i + 1) * 128);  // Конец диапазона X
                ushort c = (ushort)trackBars[i].Value;        // Значение Y для x1
                ushort d = (ushort)trackBars[i + 1].Value;    // Значение Y для x2

                // Добавляем точки в серию
                series.Points.AddXY(a, c); // Точка начала линии
                series.Points.AddXY(b, d); // Точка конца линии
            }

            // Обновление графика
            chart1.Invalidate();

            byte dp; // номер диапазона
            byte y2; // верхняя граница диапазона 0-255
            byte y1; // нижняя граница диапазона 0-255
            ushort x2; // верхняя граница диапазона 0-1023
            ushort x1; // нижняя граница диапазона 0-1023
            byte y; // итоговая яркость пискеля
            for (ushort i = 0; i < w; i++)
            {
                for (ushort j = 0; j < h; j++)
                {
                    ushort x = data[i, j];
                    x = (ushort)(x & 0x3FF);

                    dp = (byte)(x / 128);
                    y2 = (byte)trackBars[dp].Value;
                    y1 = (byte)trackBars[dp + 1].Value;
                    x2 = (ushort)(128 * dp);
                    x1 = (ushort)(128 * (dp + 1));

                    // формула для расчёта ярокости пикселя благодаря коду яркости пикселя и диапазону
                    y = (byte)((float)(x * (y2 - y1) + x2 * y1 - x1 * y2) / (x2 - x1));

                    // Установка цвета пикселя в bitmap
                    Color pixelColor = Color.FromArgb(y, y, y);
                    bitmap.SetPixel(i, j, pixelColor);
                }
            }
            // Устанавливаем bitmap как изображение для PictureBox
            pictureBox1.Image = bitmap;
        }


        private void scrollStep_ValueChanged(object sender, EventArgs e)
        {

            // Получаем текущее значение для установки шага прокрутки
            byte scrollS = (byte)scrollStep.Value;

            // Устанавливаем шаг прокрутки вручную
            panel2.VerticalScroll.SmallChange = scrollS;
            panel2.HorizontalScroll.SmallChange = scrollS;


        }
    }
}