using System;
using System.Collections.Generic;
using System.Linq;
using FinancialPlatform.Core.Interfaces;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FinancialPlatform.Infrastructure.Services
{
    /// <summary>
    /// Represents a single time-series data point for Relative Strength Index.
    /// </summary>
    public class RsiData
    {
        public float TimeIndex { get; set; }
        public float RsiValue { get; set; }
    }

    /// <summary>
    /// Represents the predicted output structure from the ML.NET Regression algorithm.
    /// </summary>
    public class RsiPrediction
    {
        [ColumnName("Score")]
        public float PredictedRsi { get; set; }
    }

    /// <summary>
    /// Contains the core Machine Learning execution pipeline for Market Prediction.
    /// Implements standard Data Science Object-Oriented patterns (Transform -> Fit -> Evaluate).
    /// </summary>
    public class MarketPredictorService : IMarketPredictorService
    {
        private readonly MLContext _mlContext;

        public MarketPredictorService()
        {
            _mlContext = new MLContext(seed: 1);
        }

        /// <summary>
        /// Orchestrates the Machine Learning pipeline to predict Market Signals.
        /// </summary>
        /// <param name="recentRsiValues">A sequence of recent RSI values to act as the Training Dataset.</param>
        /// <returns>A string representing the computed signal: "BUY", "SELL", or "HOLD".</returns>
        public string PredictSignal(IEnumerable<double> recentRsiValues)
        {
            var rsiList = recentRsiValues.ToList();
            if (rsiList.Count < 5) return "HOLD"; // Insufficient data for regression

            // Step 1: Data Transformation (Equivalent to `Transform` in Python)
            IDataView trainingData = TransformData(rsiList);

            // Step 2: Model Training (Equivalent to `Fit` in Python)
            ITransformer trainedModel = FitModel(trainingData);

            // Step 3: Inference and Evaluation (Equivalent to `Evaluate/Predict` in Python)
            return EvaluateSignal(trainedModel, rsiList);
        }

        /// <summary>
        /// Transforms raw numeric sequences into ML.NET IDataView struct format.
        /// </summary>
        private IDataView TransformData(List<double> rsiList)
        {
            var dataList = new List<RsiData>();
            for (int i = 0; i < rsiList.Count; i++)
            {
                dataList.Add(new RsiData { TimeIndex = i, RsiValue = (float)rsiList[i] });
            }
            return _mlContext.Data.LoadFromEnumerable(dataList);
        }

        /// <summary>
        /// Trains a linear statistical model using Stochastic Dual Coordinate Ascent (SDCA) Regression.
        /// </summary>
        private ITransformer FitModel(IDataView trainingData)
        {
            var pipeline = _mlContext.Transforms.Concatenate("Features", "TimeIndex")
                .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "RsiValue", featureColumnName: "Features"));
            
            return pipeline.Fit(trainingData);
        }

        /// <summary>
        /// Evaluates the trained model against the next index to predict the upcoming trend slope.
        /// </summary>
        private string EvaluateSignal(ITransformer model, List<double> rsiList)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<RsiData, RsiPrediction>(model);
            
            var nextIndex = rsiList.Count;
            var prediction = predictionEngine.Predict(new RsiData { TimeIndex = nextIndex });

            float lastRsi = (float)rsiList.Last();
            float predictedRsi = prediction.PredictedRsi;
            
            // Calculate trend slope/gradient
            float slope = predictedRsi - lastRsi;

            // Strategy: Buy the Deepest Dip if turning upward
            if (lastRsi < 40 && slope > 0.5f) return "BUY";
            
            // Strategy: Sell the Peak if turning downward
            if (lastRsi > 60 && slope < -0.5f) return "SELL";

            return "HOLD";
        }
    }
}
