//---------------------------------------------------------------------
// <copyright file="VsMemoryUsageComparer.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace ResultsComparer.VsProfiler
{
    /// <summary>
    /// An <see cref="Core.IResultsComparer"/> that can compare
    /// the Type Allocations reports generated by the VS .NET Object Allocation Tracker.
    /// These reports are generated by selecting the desired rows in the VS UI, right-clicking
    /// then copy-pasting the data into a text file.
    /// </summary>
    public class VsMemoryUsageComparer : VsProfilerComparer<VsProfilerMemoryUsage>
    {
        /// <inheritdoc/>
        public override string Name => "VS Memory Usage";

        /// <inheritdoc/>
        protected override IDictionary<string, string> MetricNameMap => new Dictionary<string, string>()
        {
            { "Count", "Count" },
            { "Size", "Size (bytes)" },
            { "Bytes", "Size (bytes)" },
            { "Inclusive Size", "Inclusive Size (bytes)" },
            { "Inclusive Bytes", "Inclusive Size (bytes)" }
        };

        /// <inheritdoc/>
        protected override string DefaultMetric => "Count";

        /// <inheritdoc/>
        public override bool CanReadFile(string path)
        {
            using var reader = new StreamReader(path);
            string firstLine = reader.ReadLine();
            return firstLine != null
                && firstLine.Contains("Object Type")
                && (firstLine.Contains("Count") || firstLine.Contains("Inclusive Size"));
        }

        /// <inheritdoc/>
        protected override string GetItemId(VsProfilerMemoryUsage item)
        {
            return item.Type;
        }

        /// <inheritdoc/>
        protected override long GetMetricValue(VsProfilerMemoryUsage memoryUsage, string metric)
        {
            if (metric.Equals("Count", StringComparison.OrdinalIgnoreCase))
            {
                return memoryUsage.Count;
            }
            else if (metric.Equals("Size", StringComparison.OrdinalIgnoreCase) || metric.Equals("Bytes", StringComparison.OrdinalIgnoreCase))
            {
                return memoryUsage.Size;
            }
            else if (metric.Equals("Inclusive Size", StringComparison.OrdinalIgnoreCase) || metric.Equals("Inclusive Bytes", StringComparison.OrdinalIgnoreCase))
            {
                return memoryUsage.InclusiveSize;
            }

            throw new Exception($"Unsupported metric {metric} for VS Profiler Allocations Comparer");
        }
    }
}
