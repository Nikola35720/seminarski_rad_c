using CsvChartClient.Models;
using CsvChartClient.ViewModels;
using System.Diagnostics;

namespace CsvChartClient.Services;

public class ChartDrawable : IDrawable
{
    private readonly MainViewModel _vm;

    private static readonly Color[] Palette =
    {
        Color.FromArgb("#1f77b4"), Color.FromArgb("#ff7f0e"),
        Color.FromArgb("#2ca02c"), Color.FromArgb("#d62728"),
        Color.FromArgb("#9467bd"), Color.FromArgb("#8c564b"),
        Color.FromArgb("#e377c2"), Color.FromArgb("#17becf"),
        Color.FromArgb("#bcbd22"), Color.FromArgb("#7f7f7f"),
        Color.FromArgb("#aec7e8"), Color.FromArgb("#ffbb78"),
        Color.FromArgb("#98df8a"), Color.FromArgb("#ff9896"),
        Color.FromArgb("#c5b0d5"), Color.FromArgb("#c49c94"),
        Color.FromArgb("#f7b6d2"), Color.FromArgb("#c7c7c7"),
        Color.FromArgb("#dbdb8d"), Color.FromArgb("#9edae5")
    };

    private const int MaxXLabels = 15;
    private const int MaxSlices = 8;

    public ChartDrawable(MainViewModel vm) => _vm = vm;

    public void Draw(ICanvas canvas, RectF rect)
    {
        var data = _vm.GetDisplayData();
        var series = _vm.SeriesNames;
        var visible = _vm.VisibleSeriesIndices;

        Debug.WriteLine($"[DRAW] {data.Count} redova, {series.Count} serija, {visible.Count} vidljivo, tip={_vm.ChartType}");

        if (data.Count == 0 || series.Count == 0 || visible.Count == 0) return;

        switch (_vm.ChartType)
        {
            case 0: DrawLine(canvas, rect, data, series, visible); break;
            case 1: DrawColumn(canvas, rect, data, series, visible); break;
            case 2: DrawPie(canvas, rect, data, series, visible); break;
        }
    }

    private static (double min, double max) GetRange(IList<DataPoint> data, IList<int> visible)
    {
        double max = double.MinValue;
        double min = double.MaxValue;
        foreach (var dp in data)
            foreach (var idx in visible)
            {
                if (idx >= dp.Values.Count) continue;
                double v = dp.Values[idx];
                if (v > max) max = v;
                if (v < min) min = v;
            }
        if (min > 0) min = 0;
        if (max == min) max = min + 1;
        return (min, max);
    }

    private void DrawLegend(ICanvas canvas, RectF r, IList<string> series, IList<int> visible)
    {
        float x = 80;
        float y = 8;
        float maxX = r.Width - 20;
        float itemWidth = 150;
        canvas.FontSize = 12;

        foreach (var idx in visible)
        {
            if (x + itemWidth > maxX) { x = 80; y += 20; }

            canvas.FillColor = Palette[idx % Palette.Length];
            canvas.FillRectangle(x, y, 14, 14);

            canvas.FontColor = Colors.Black;
            canvas.DrawString(series[idx],
                x + 18, y - 2, 120, 18,
                HorizontalAlignment.Left, VerticalAlignment.Center);

            x += itemWidth;
        }
    }

    private static int GetLabelStep(int total)
    {
        if (total <= MaxXLabels) return 1;
        return (int)Math.Ceiling((double)total / MaxXLabels);
    }

    private static Color GetContrastColor(Color bg)
    {
        double lum = 0.299 * bg.Red + 0.587 * bg.Green + 0.114 * bg.Blue;
        return lum > 0.5 ? Colors.Black : Colors.White;
    }

    private static Color GenerateColor(int index, int total)
    {
        double hue = (index * 360.0 / Math.Max(1, total)) % 360;
        return Color.FromHsla(hue / 360.0, 0.7, 0.5);
    }

    private void DrawLine(ICanvas canvas, RectF r, IList<DataPoint> data, IList<string> series, IList<int> visible)
    {
        float left = 70, right = r.Width - 30;
        float top = 40, bottom = r.Height - 60;
        float w = right - left, h = bottom - top;

        var (min, max) = GetRange(data, visible);

        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1;
        canvas.DrawLine(left, bottom, right, bottom);
        canvas.DrawLine(left, top, left, bottom);

        canvas.FontSize = 11;
        canvas.FontColor = Colors.Black;
        for (int i = 0; i <= 5; i++)
        {
            float y = bottom - i * h / 5;
            double vrednost = min + i * (max - min) / 5;
            canvas.DrawString(vrednost.ToString("0.##"),
                left - 60, y - 8, 55, 16,
                HorizontalAlignment.Right, VerticalAlignment.Center);
            canvas.StrokeColor = Colors.LightGray;
            canvas.DrawLine(left - 4, y, left, y);
        }

        foreach (var idx in visible)
        {
            canvas.StrokeColor = Palette[idx % Palette.Length];
            canvas.StrokeSize = 2;
            var path = new PathF();

            for (int i = 0; i < data.Count; i++)
            {
                if (idx >= data[i].Values.Count) continue;
                float x = left + (data.Count == 1 ? w / 2 : i * w / (data.Count - 1));
                float y = bottom - (float)((data[i].Values[idx] - min) / (max - min)) * h;
                if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
            }
            canvas.DrawPath(path);

            canvas.FillColor = Palette[idx % Palette.Length];
            for (int i = 0; i < data.Count; i++)
            {
                if (idx >= data[i].Values.Count) continue;
                float x = left + (data.Count == 1 ? w / 2 : i * w / (data.Count - 1));
                float y = bottom - (float)((data[i].Values[idx] - min) / (max - min)) * h;
                canvas.FillCircle(x, y, 3);
            }
        }

        canvas.FontColor = Colors.Black;
        canvas.FontSize = 11;
        int step = GetLabelStep(data.Count);
        for (int i = 0; i < data.Count; i += step)
        {
            float x = left + (data.Count == 1 ? w / 2 : i * w / (data.Count - 1));
            canvas.DrawString(data[i].Label,
                x - 40, bottom + 5, 80, 20,
                HorizontalAlignment.Center, VerticalAlignment.Top);
        }

        canvas.FontSize = 12;
        canvas.FontColor = Colors.Black;
        canvas.DrawString("Vrednost",
            left - 65, top - 25, 100, 20,
            HorizontalAlignment.Left, VerticalAlignment.Center);
        canvas.DrawString($"X: {_vm.XAxisColumn}",
            right - 150, bottom + 30, 150, 20,
            HorizontalAlignment.Right, VerticalAlignment.Top);

        DrawLegend(canvas, r, series, visible);
    }

    private void DrawColumn(ICanvas canvas, RectF r, IList<DataPoint> data, IList<string> series, IList<int> visible)
    {
        float left = 70, right = r.Width - 30;
        float top = 40, bottom = r.Height - 60;
        float w = right - left, h = bottom - top;

        IList<DataPoint> displayData = data;

        var (min, max) = GetRange(displayData, visible);
        max = Math.Max(1, max);

        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1;
        canvas.DrawLine(left, bottom, right, bottom);
        canvas.DrawLine(left, top, left, bottom);

        canvas.FontSize = 11;
        canvas.FontColor = Colors.Black;
        for (int i = 0; i <= 5; i++)
        {
            float y = bottom - i * h / 5;
            double vrednost = i * max / 5;
            canvas.DrawString(vrednost.ToString("0.##"),
                left - 60, y - 8, 55, 16,
                HorizontalAlignment.Right, VerticalAlignment.Center);
            canvas.StrokeColor = Colors.LightGray;
            canvas.DrawLine(left - 4, y, left, y);
        }

        float grupaW = w / displayData.Count;
        float stubW = grupaW / (visible.Count + 1);

        bool showLabels = displayData.Count <= MaxXLabels;
        int labelStep = GetLabelStep(displayData.Count);

        for (int i = 0; i < displayData.Count; i++)
        {
            int slot = 0;
            foreach (var idx in visible)
            {
                if (idx >= displayData[i].Values.Count) { slot++; continue; }

                float barH = (float)(displayData[i].Values[idx] / max) * h;
                float x = left + i * grupaW + stubW * (slot + 0.5f);
                float y = bottom - barH;

                canvas.FillColor = Palette[idx % Palette.Length];
                canvas.FillRectangle(x, y, stubW * 0.9f, barH);
                slot++;
            }

            if (showLabels || i % labelStep == 0)
            {
                canvas.FontColor = Colors.Black;
                canvas.FontSize = 11;
                canvas.DrawString(displayData[i].Label,
                    left + i * grupaW, bottom + 5, grupaW, 20,
                    HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        canvas.FontSize = 12;
        canvas.FontColor = Colors.Black;
        canvas.DrawString("Vrednost",
            left - 65, top - 25, 100, 20,
            HorizontalAlignment.Left, VerticalAlignment.Center);
        canvas.DrawString($"X: {_vm.XAxisColumn}",
            right - 150, bottom + 30, 150, 20,
            HorizontalAlignment.Right, VerticalAlignment.Top);

        DrawLegend(canvas, r, series, visible);
    }

    private void DrawPie(ICanvas canvas, RectF r, IList<DataPoint> data, IList<string> series, IList<int> visible)
    {
        if (data.Count == 0 || series.Count == 0) return;

        if (visible.Count == 0)
        {
            canvas.FontColor = Colors.Gray;
            canvas.FontSize = 14;
            canvas.DrawString("Cekirajte bar jednu seriju",
                r.Width / 2 - 120, r.Height / 2 - 10, 240, 20,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        int seriesIndex = visible[0];

        var items = new List<(string label, double value, int index)>();
        for (int i = 0; i < data.Count; i++)
        {
            if (seriesIndex >= data[i].Values.Count) continue;
            double v = data[i].Values[seriesIndex];
            if (v > 0) items.Add((data[i].Label, v, i));
        }

        if (items.Count == 0) return;

        if (items.Count > MaxSlices)
        {
            var sorted = items.OrderByDescending(x => x.value).ToList();
            var top = sorted.Take(MaxSlices - 1).ToList();
            var rest = sorted.Skip(MaxSlices - 1).ToList();
            double restSum = rest.Sum(x => x.value);
            items = top;
            items.Add(($"Ostalo ({rest.Count})", restSum, -1));
        }

        double total = items.Sum(x => x.value);
        if (total <= 0) return;

        int legendRows = (int)Math.Ceiling(items.Count / 3.0);
        float legendHeight = legendRows * 20 + 10;
        float availableH = r.Height - legendHeight - 50;
        float availableW = r.Width - 40;
        float radius = Math.Max(30, Math.Min(availableW, availableH) / 2);

        float cx = r.Width / 2;
        float cy = 40 + radius;

        canvas.FontColor = Colors.Black;
        canvas.FontSize = 13;
        canvas.DrawString($"Prikaz: {series[seriesIndex]}",
            0, 10, r.Width, 20,
            HorizontalAlignment.Center, VerticalAlignment.Top);

        float start = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var (label, value, _) = items[i];
            float sweep = (float)(value / total * 360);

            canvas.FillColor = GenerateColor(i, items.Count);
            canvas.FillArc(cx - radius, cy - radius, radius * 2, radius * 2,
                           start, sweep, true);
            start += sweep;
        }

        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 1;
        start = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var (label, value, _) = items[i];
            float sweep = (float)(value / total * 360);
            double rad = start * Math.PI / 180;
            float x2 = cx + (float)(Math.Cos(rad) * radius);
            float y2 = cy + (float)(Math.Sin(rad) * radius);
            canvas.DrawLine(cx, cy, x2, y2);
            start += sweep;
        }

        start = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var (label, value, _) = items[i];
            float sweep = (float)(value / total * 360);
            double procenat = value / total * 100;

            if (procenat >= 5)
            {
                double midRad = (start + sweep / 2) * Math.PI / 180;
                float lx = cx + (float)(Math.Cos(midRad) * radius * 0.6);
                float ly = cy + (float)(Math.Sin(midRad) * radius * 0.6);

                canvas.FontColor = GetContrastColor(GenerateColor(i, items.Count));
                canvas.FontSize = 12;
                canvas.DrawString($"{procenat:0.#}%",
                    lx - 30, ly - 8, 60, 16,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            start += sweep;
        }

        float legendTop = cy + radius + 15;
        float legendX = 20;
        float legendY = legendTop;
        float itemWidth = 200;
        float maxX = r.Width - 20;

        canvas.FontSize = 11;
        for (int i = 0; i < items.Count; i++)
        {
            var (label, value, _) = items[i];
            double procenat = value / total * 100;

            if (legendX + itemWidth > maxX)
            {
                legendX = 20;
                legendY += 18;
            }

            canvas.FillColor = GenerateColor(i, items.Count);
            canvas.FillRectangle(legendX, legendY, 12, 12);

            canvas.FontColor = Colors.Black;
            canvas.DrawString($"{label} ({procenat:0.#}%)",
                legendX + 16, legendY - 2, itemWidth - 18, 16,
                HorizontalAlignment.Left, VerticalAlignment.Center);

            legendX += itemWidth;
        }
    }
}