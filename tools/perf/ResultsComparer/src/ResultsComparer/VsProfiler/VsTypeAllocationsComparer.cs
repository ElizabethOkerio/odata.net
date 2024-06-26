﻿//---------------------------------------------------------------------
// <copyright file="VsTypeAllocationsComparer.cs" company="Microsoft">
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
    public class VsTypeAllocationsComparer : VsProfilerComparer<VsProfilerTypeAllocations>
    {
        /// <inheritdoc/>
        public override string Name => "VS .NET Object Allocations - Functions";

        /// <inheritdoc/>
        protected override IDictionary<string, string> MetricNameMap => new Dictionary<string, string>()
        {
            { "Allocations", "Allocations" },
            { "Size", "Size (bytes)" },
            { "Bytes", "Size (bytes)" }
        };

        /// <inheritdoc/>
        protected override string DefaultMetric => "Allocations";

        /// <inheritdoc/>
        public override bool CanReadFile(string path)
        {
            using var reader = new StreamReader(path);
            string firstLine = reader.ReadLine();
            return firstLine != null
                && firstLine.Contains("Type")
                && (firstLine.Contains("Allocations") || firstLine.Contains("Bytes"));
        }
        /// <inheritdoc/>
        protected override string GetItemId(VsProfilerTypeAllocations item)
        {
            return item.Type;
        }

        /// <inheritdoc/>
        protected override long GetMetricValue(VsProfilerTypeAllocations allocations, string metric)
        {
            if (metric.Equals("Allocations", StringComparison.OrdinalIgnoreCase))
            {
                return allocations.Allocations;
            }
            else if (metric.Equals("Size", StringComparison.OrdinalIgnoreCase) || metric.Equals("Bytes", StringComparison.OrdinalIgnoreCase))
            {
                return allocations.Bytes;
            }

            throw new Exception($"Unsupported metric {metric} for VS Profiler Allocations Comparer");
        }
    }
}
