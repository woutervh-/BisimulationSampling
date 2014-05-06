using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;

namespace GraphTools.Plot
{
    /// <summary>
    /// Represents a line series with error bar options.
    /// </summary>
    class ErrorLineSeries : LineSeries
    {
        /// <summary>
        /// Error values for each point in the line plot.
        /// </summary>
        public IList<double> ErrorValues { get; set; }

        /// <summary>
        /// Width of the error bars.
        /// </summary>
        public double ErrorWidth { get; set; }

        /// <summary>
        /// Renders the series on the specified rendering context.
        /// </summary>
        /// <param name="rc"></param>
        /// <param name="model"></param>
        public override void Render(IRenderContext rc, PlotModel model)
        {
            // Check if number of error points is equal to number of data points
            if (Points.Count != ErrorValues.Count)
            {
                throw new InvalidOperationException();
            }

            // Render data line
            base.Render(rc, model);

            // Render error points
            for (int i = 0; i < Points.Count; i++)
            {
                var x = Points[i].X;
                var y = Points[i].Y;
                var error = ErrorValues[i];
                var low = y - error;
                var high = y + error;

                var leftValue = x - 0.5 * ErrorWidth;
                var middleValue = x;
                var rightValue = x + 0.5 * ErrorWidth;

                var lowerErrorPoint = this.Transform(middleValue, low);
                var upperErrorPoint = this.Transform(middleValue, high);

                // Draw vertical bar
                rc.DrawClippedLine
                (
                    new List<ScreenPoint> { lowerErrorPoint, upperErrorPoint },
                    GetClippingRect(),
                    0.0,
                    ActualColor,
                    1.0,
                    LineStyle.Solid,
                    OxyPenLineJoin.Miter,
                    true
                );

                // Draw bottom horizontal bar
                var lowerLeftErrorPoint = this.Transform(leftValue, low);
                var lowerRightErrorPoint = this.Transform(rightValue, low);
                rc.DrawClippedLine
                (
                    new List<ScreenPoint> { lowerLeftErrorPoint, lowerRightErrorPoint },
                    GetClippingRect(),
                    0,
                    ActualColor,
                    1.0,
                    LineStyle.Solid,
                    OxyPenLineJoin.Miter,
                    true
                );

                // Draw top horizonal bar
                var upperLeftErrorPoint = this.Transform(leftValue, high);
                var upperRightErrorPoint = this.Transform(rightValue, high);
                rc.DrawClippedLine
                (
                    new List<ScreenPoint> { upperLeftErrorPoint, upperRightErrorPoint },
                    GetClippingRect(),
                    0,
                    ActualColor,
                    1.0,
                    LineStyle.Solid,
                    OxyPenLineJoin.Miter,
                    true
                );
            }
        }
    }
}
