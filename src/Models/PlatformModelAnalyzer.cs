using System;
using System.Collections.Generic;

namespace AvionPlot.Models
{
    public class PlatformModel
    {
        // Параметры системы
        public double Mass { get; set; } = 1000;       // приведённая масса [кг]
        public double Stiffness { get; set; } = 50000; // жёсткость [Н/м]
        public double Damping { get; set; } = 1000;    // коэффициент демпфирования [Н·с/м]

        public double NaturalFrequency => Math.Sqrt(Stiffness / Mass);

        public double DampingRatio => Damping / (2 * Math.Sqrt(Mass * Stiffness));

        public double SettlingTime => 3.0 / (DampingRatio * NaturalFrequency);

        public double PeakAmplitude(double initialForce) =>
            initialForce / Stiffness * Math.Exp(-DampingRatio * NaturalFrequency * SettlingTime);

        public string GetFormulasText() =>
            $"Математическая модель второго порядка:\n" +
            $"m·x'' + c·x' + k·x = F(t)\n\n" +
            $"Параметры платформы:\n" +
            $"Масса: {Mass} кг\n" +
            $"Жёсткость: {Stiffness} Н/м\n" +
            $"Демпфирование: {Damping} Н·с/м\n\n" +
            $"Собственная частота: {NaturalFrequency:F2} рад/с\n" +
            $"Коэффициент затухания: {DampingRatio:F2}\n" +
            $"Время установления: {SettlingTime:F2} с";
    }

    public static class GraphAnalyzer
    {
        /// <summary>
        /// Анализ списка значений оси и вычисление коэффициента демпфирования и собственной частоты
        /// </summary>
        /// <param name="values">Список значений (например OSWES)</param>
        /// <param name="timeStep">Шаг времени между точками</param>
        /// <returns>(dampingRatio, omega_n)</returns>
        public static (double dampingRatio, double naturalFreq) AnalyzeSeries(List<double> values, double timeStep = 0.01)
        {
            if (values == null || values.Count < 3) return (0, 0);

            // Находим пики (локальные максимумы)
            var peaks = new List<int>();
            for (int i = 1; i < values.Count - 1; i++)
            {
                if (values[i] > values[i - 1] && values[i] > values[i + 1])
                    peaks.Add(i);
            }

            if (peaks.Count < 2) return (0, 0);

            // Логарифмический декремент
            double delta = Math.Log(values[peaks[0]] / values[peaks[1]]);
            double dt = (peaks[1] - peaks[0]) * timeStep;
            double dampingRatio = delta / Math.Sqrt(4 * Math.PI * Math.PI + delta * delta);

            // Затухающая частота
            double period = (peaks[1] - peaks[0]) * timeStep;
            double omega_d = 2 * Math.PI / period;
            double omega_n = omega_d / Math.Sqrt(1 - dampingRatio * dampingRatio);

            return (dampingRatio, omega_n);
        }
    }
}
