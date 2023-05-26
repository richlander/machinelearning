﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.ML.SearchSpace;

namespace Microsoft.ML.AutoML
{
    /// <summary>
    /// Interface for dataset manager. This interface doesn't include any method or property definition and is used by <see cref="AutoMLExperiment"/> and other components to retrieve the instance of the actual
    /// dataset manager from containers.
    /// </summary>
    public interface IDatasetManager
    {
    }

    public interface ICrossValidateDatasetManager
    {
        int? Fold { get; set; }

        IDataView Dataset { get; set; }

        string SamplingKeyColumnName { get; set; }
    }

    public interface ITrainValidateDatasetManager
    {
        IDataView LoadTrainDataset(MLContext context, TrialSettings settings);

        IDataView LoadValidateDataset(MLContext context, TrialSettings settings);
    }

    internal class TrainValidateDatasetManager : IDatasetManager, ITrainValidateDatasetManager
    {
        private ulong _rowCount;
        private IDataView _trainDataset;
        private readonly IDataView _validateDataset;
        private readonly string _subSamplingKey = "TrainValidateDatasetSubsamplingKey";
        private bool _isInitialized = false;
        public TrainValidateDatasetManager(IDataView trainDataset, IDataView validateDataset, string subSamplingKey = null)
        {
            _trainDataset = trainDataset;
            _validateDataset = validateDataset;
            _subSamplingKey = subSamplingKey ?? _subSamplingKey;
        }

        public string SubSamplingKey => _subSamplingKey;

        public IDataView TrainDataset { get; set; }

        public IDataView ValidateDataset { get; set; }

        /// <summary>
        /// Load Train Dataset. If <see cref="TrialSettings.Parameter"/> contains <see cref="_subSamplingKey"/> then the train dataset will be subsampled.
        /// </summary>
        /// <returns>train dataset.</returns>
        public IDataView LoadTrainDataset(MLContext context, TrialSettings settings)
        {
            if (!_isInitialized)
            {
                InitializeTrainDataset(context);
                _isInitialized = true;
            }
            var trainTestSplitParameter = settings.Parameter.ContainsKey(nameof(TrainValidateDatasetManager)) ? settings.Parameter[nameof(TrainValidateDatasetManager)] : null;
            if (trainTestSplitParameter is Parameter parameter)
            {
                var subSampleRatio = parameter.ContainsKey(_subSamplingKey) ? parameter[_subSamplingKey].AsType<double>() : 1;
                if (subSampleRatio < 1.0)
                {
                    var subSampledTrainDataset = context.Data.TakeRows(_trainDataset, (long)(subSampleRatio * _rowCount));
                    return subSampledTrainDataset;
                }
            }

            return _trainDataset;
        }

        public IDataView LoadValidateDataset(MLContext context, TrialSettings settings)
        {
            return _validateDataset;
        }

        private void InitializeTrainDataset(MLContext context)
        {
            _rowCount = DatasetDimensionsUtil.CountRows(_trainDataset, ulong.MaxValue);
            _trainDataset = context.Data.ShuffleRows(_trainDataset);
        }
    }

    internal class CrossValidateDatasetManager : IDatasetManager, ICrossValidateDatasetManager
    {
        public IDataView Dataset { get; set; }

        public int? Fold { get; set; }

        public string SamplingKeyColumnName { get; set; }
    }
}
