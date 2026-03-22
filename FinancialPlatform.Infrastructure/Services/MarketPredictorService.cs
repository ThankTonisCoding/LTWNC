using System;
using System.Collections.Generic;
using System.Linq;
using FinancialPlatform.Core.Interfaces;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FinancialPlatform.Infrastructure.Services
{
    public class RsiData
    {
        public float TimeIndex { get; set; }
        public float RsiValue { get; set; }
    }

    public class RsiPrediction
    {
        [ColumnName("Score")]
        public float PredictedRsi { get; set; }
    }

    public class MarketPredictorService : IMarketPredictorService
    {
        private readonly MLContext _mlContext;

        public MarketPredictorService()
        {
            _mlContext = new MLContext(seed: 1);
        }

        public string PredictSignal(IEnumerable<double> recentRsiValues)
        {
            var rsiList = recentRsiValues.ToList();
            if (rsiList.Count < 5) return "HOLD"; // Quá ít dữ liệu để Train

            // 1. Chuyển đổi dữ liệu sang định dạng ML.NET học
            var dataList = new List<RsiData>();
            for (int i = 0; i < rsiList.Count; i++)
            {
                dataList.Add(new RsiData { TimeIndex = i, RsiValue = (float)rsiList[i] });
            }

            // 2. Tải dữ liệu vào IDataView
            IDataView trainingData = _mlContext.Data.LoadFromEnumerable(dataList);

            // 3. Cấu hình Pipeline Hồi quy tuyến tính (Dùng Sdca mặc định của ML.NET dể tìm Trend)
            var pipeline = _mlContext.Transforms.Concatenate("Features", "TimeIndex")
                .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "RsiValue", featureColumnName: "Features"));

            // 4. Đào tạo Model AI (Siêu tốc vì chỉ có vài dữ liệu)
            var model = pipeline.Fit(trainingData);

            // 5. Dự đoán RSI tiếp theo
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<RsiData, RsiPrediction>(model);
            
            var nextIndex = rsiList.Count;
            var prediction = predictionEngine.Predict(new RsiData { TimeIndex = nextIndex });

            float lastRsi = (float)rsiList.Last();
            float predictedRsi = prediction.PredictedRsi;
            
            // 6. Xây dựng logic Khuyến nghị Mua/Bán dựa trên AI Prediction
            float slope = predictedRsi - lastRsi;

            // Nếu RSI nhỏ (đang giảm sâu) mà cắm đầu quay lên mạn (Độ dốc cực dương) -> AI báo MUA
            if (lastRsi < 40 && slope > 0.5f)
            {
                return "BUY";
            }
            // Nếu RSI lớn (đang tăng quá nóng) mà cắm đầu quay xuống (Độ dốc cực âm) -> AI báo BÁN
            if (lastRsi > 60 && slope < -0.5f)
            {
                return "SELL";
            }

            return "HOLD";
        }
    }
}
