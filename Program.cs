using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    // ========== 配置 ==========
    static readonly string PUSH_TOKEN = Environment.GetEnvironmentVariable("PUSH_TOKEN") ?? "";
    static readonly string WEATHER_KEY = Environment.GetEnvironmentVariable("WEATHER_KEY") ?? "";
    static readonly string CITY_ID = Environment.GetEnvironmentVariable("CITY_ID") ?? "101010100";
    static readonly string CITY_NAME = Environment.GetEnvironmentVariable("CITY_NAME") ?? "北京";
    static readonly string START_DATE = Environment.GetEnvironmentVariable("START_DATE") ?? "";

    static readonly HttpClient http = new HttpClient();
    static readonly Random rnd = new Random();

    // 城市经纬度表（Open-Meteo 备用源）
    static readonly Dictionary<string, (double lat, double lon)> CITY_COORDS = new()
    {
        ["101010100"] = (39.90, 116.41), ["101020100"] = (31.23, 121.47),
        ["101280101"] = (23.13, 113.26), ["101280601"] = (22.54, 114.06),
        ["101030100"] = (39.13, 117.20), ["101040100"] = (29.56, 106.55),
        ["101050101"] = (45.75, 126.63), ["101060101"] = (43.82, 125.32),
        ["101070101"] = (41.80, 123.43), ["101080101"] = (40.84, 111.75),
        ["101090101"] = (38.04, 114.51), ["101100101"] = (37.87, 112.55),
        ["101110101"] = (34.27, 108.95), ["101120101"] = (36.65, 117.12),
        ["101130101"] = (43.79, 87.62),  ["101140101"] = (36.06, 103.83),
        ["101150101"] = (36.62, 101.78), ["101160101"] = (38.49, 106.23),
        ["101170101"] = (36.06, 120.38), ["101180101"] = (34.75, 113.65),
        ["101190101"] = (32.06, 118.78), ["101200101"] = (30.59, 114.31),
        ["101210101"] = (30.25, 120.16), ["101220101"] = (31.86, 117.28),
        ["101230101"] = (26.08, 119.30), ["101240101"] = (28.68, 115.88),
        ["101250101"] = (28.23, 112.98), ["101260101"] = (26.65, 106.63),
        ["101270101"] = (30.67, 104.07), ["101280201"] = (23.02, 113.12),
        ["101280401"] = (22.27, 113.57), ["101290101"] = (25.04, 102.71),
        ["101300101"] = (22.82, 108.32), ["101310101"] = (20.02, 110.35),
        ["101320101"] = (22.32, 114.17), ["101330101"] = (22.20, 113.55),
        ["101340101"] = (25.03, 121.56),
    };

    // WMO 天气代码映射
    static readonly Dictionary<int, string> WMO_WEATHER = new()
    {
        [0] = "晴", [1] = "晴", [2] = "多云", [3] = "阴",
        [45] = "雾", [48] = "雾凇",
        [51] = "毛毛雨", [53] = "小雨", [55] = "中雨",
        [56] = "冻雨", [57] = "冻雨",
        [61] = "小雨", [63] = "中雨", [65] = "大雨",
        [66] = "冻雨", [67] = "冻雨",
        [71] = "小雪", [73] = "中雪", [75] = "大雪",
        [77] = "雪粒",
        [80] = "阵雨", [81] = "强阵雨", [82] = "暴雨",
        [85] = "阵雪", [86] = "强阵雪",
        [95] = "雷阵雨", [96] = "雷阵雨伴冰雹", [99] = "强雷阵雨伴冰雹",
    };

    // 情话库
    static readonly string[] LOVE_QUOTES = new[]
    {
        "今天也是爱你的一天，比昨天多一点，比明天少一点。",
        "你是我这辈子最美的意外。",
        "遇见你之后，我再也没羡慕过别人。",
        "你是我所有温柔的来源和归属。",
        "世界那么大，遇见你真好。",
        "你是我心上的月亮，也是眼里的星光。",
        "今天也要记得想我哦，我一直在想你。",
        "你是我绕过山河错落，才找到的人间烟火。",
        "喜欢你这件事，我想让全世界都知道。",
        "你是我疲惫生活中的英雄梦想。",
        "每天醒来，觉得甚是爱你。",
        "你是我这一生等了半世未拆的礼物。",
        "我想和你虚度时光，比如低头看鱼。",
        "你是我所有的少女情怀和心之所向。",
        "山河远阔，人间烟火，无一是你，无一不是你。",
        "你是我明目张胆的偏爱，也是我众所周知的私心。",
        "想把世界上最好的都给你，却发现世界上最好的就是你。",
        "你是我温暖的手套，冰冷的啤酒，带着阳光味道的衬衫。",
        "自从遇见你，人生苦短，甜长。",
        "你是我纸短情长的雨季，也是我往后余生的晴空万里。",
        "我想和你一起生活，在某个小镇，共享无尽的黄昏。",
        "你是我这一生只会遇见一次的惊喜。",
        "你是我心之所向，素履以往。",
        "我想和你互相浪费，一起虚度短的沉默，长的无意义。",
        "你是我所有故事里的主角。",
        "你是我翻遍银河要找的那颗星。",
        "你是我藏在云层里的月亮，也是我穷极一生寻找的宝藏。",
        "你是我褪去新鲜感后仍然心动的人。",
        "我想和你一起，把日子过成诗。",
        "你是我平淡岁月里的星辰。",
    };

    static async Task Main(string[] args)
    {
        Console.WriteLine("🌤 开始获取天气并推送...");

        if (string.IsNullOrEmpty(PUSH_TOKEN))
        {
            Console.WriteLine("❌ 错误：未设置 PUSH_TOKEN 环境变量");
            return;
        }
        if (string.IsNullOrEmpty(CITY_ID))
        {
            Console.WriteLine("❌ 错误：未设置 CITY_ID 环境变量");
            return;
        }

        // 先尝试和风天气
        WeatherData? now = null;
        ForecastData? forecast = null;
        string source = "";

        if (!string.IsNullOrEmpty(WEATHER_KEY))
        {
            Console.WriteLine("🔄 尝试和风天气...");
            (now, forecast) = await GetWeatherQweatherAsync();
            if (now != null)
            {
                source = "和风天气";
                Console.WriteLine("✅ 和风天气获取成功");
            }
            else
            {
                Console.WriteLine("⚠️ 和风天气获取失败，切换到备用源...");
            }
        }

        // 备用源
        if (now == null)
        {
            Console.WriteLine("🔄 尝试 Open-Meteo 备用源...");
            (now, forecast) = await GetWeatherOpenMeteoAsync();
            if (now != null)
            {
                source = "Open-Meteo";
                Console.WriteLine("✅ Open-Meteo 获取成功");
            }
        }

        if (now == null)
        {
            Console.WriteLine("❌ 所有天气源都失败了，任务终止");
            return;
        }

        Console.WriteLine($"📡 数据来源: {source}");
        var (title, content) = BuildMessage(now, forecast);
        Console.WriteLine($"\n📨 推送标题: {title}");
        Console.WriteLine($"📨 推送内容预览:\n{content}");

        await PushMessageAsync(title, content);
        Console.WriteLine("🎉 任务执行完毕！");
    }

    // ========== 和风天气 ==========
    static async Task<(WeatherData?, ForecastData?)> GetWeatherQweatherAsync()
    {
        try
        {
            var nowUrl = $"https://devapi.qweather.com/v7/weather/now?location={CITY_ID}&key={WEATHER_KEY}";
            var nowResp = await http.GetStringAsync(nowUrl);
            var nowJson = JsonDocument.Parse(nowResp);
            var code = nowJson.RootElement.GetProperty("code").GetString();
            if (code != "200")
            {
                Console.WriteLine($"和风天气实时天气失败: {nowResp}");
                return (null, null);
            }

            var now = nowJson.RootElement.GetProperty("now");
            var nowData = new WeatherData
            {
                Temp = now.GetProperty("temp").GetString() ?? "--",
                FeelsLike = now.GetProperty("feelsLike").GetString() ?? "--",
                Humidity = now.GetProperty("humidity").GetString() ?? "--",
                Text = now.GetProperty("text").GetString() ?? "未知",
                WindDir = now.GetProperty("windDir").GetString() ?? "--",
                WindScale = now.GetProperty("windScale").GetString() ?? "--",
                Vis = now.TryGetProperty("vis", out var visProp) ? visProp.GetString() : "--",
            };

            var forecastUrl = $"https://devapi.qweather.com/v7/weather/3d?location={CITY_ID}&key={WEATHER_KEY}";
            var forecastResp = await http.GetStringAsync(forecastUrl);
            var forecastJson = JsonDocument.Parse(forecastResp);
            var fcode = forecastJson.RootElement.GetProperty("code").GetString();
            ForecastData? forecastData = null;
            if (fcode == "200" && forecastJson.RootElement.TryGetProperty("daily", out var dailyArr) && dailyArr.GetArrayLength() > 0)
            {
                var d0 = dailyArr[0];
                forecastData = new ForecastData
                {
                    TempMax = d0.GetProperty("tempMax").GetString() ?? "--",
                    TempMin = d0.GetProperty("tempMin").GetString() ?? "--",
                };
            }

            return (nowData, forecastData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"和风天气请求异常: {ex.Message}");
            return (null, null);
        }
    }

    // ========== Open-Meteo 备用源 ==========
    static async Task<(WeatherData?, ForecastData?)> GetWeatherOpenMeteoAsync()
    {
        if (!CITY_COORDS.TryGetValue(CITY_ID, out var coords))
        {
            Console.WriteLine($"未找到城市 {CITY_ID} 的经纬度，无法使用备用天气源");
            return (null, null);
        }

        try
        {
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={coords.lat}&longitude={coords.lon}" +
                      $"&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m,wind_direction_10m,visibility" +
                      $"&daily=temperature_2m_max,temperature_2m_min,weather_code" +
                      $"&timezone=Asia%2FShanghai";

            var resp = await http.GetStringAsync(url);
            var json = JsonDocument.Parse(resp);

            var current = json.RootElement.GetProperty("current");
            var daily = json.RootElement.GetProperty("daily");

            int wmoCode = current.GetProperty("weather_code").GetInt32();
            string weatherText = WMO_WEATHER.TryGetValue(wmoCode, out var wt) ? wt : "未知";

            double windDeg = current.GetProperty("wind_direction_10m").GetDouble();
            string windDir = AngleToDirection(windDeg);

            double windSpeed = current.GetProperty("wind_speed_10m").GetDouble();
            int windScale = MsToScale(windSpeed);

            var nowData = new WeatherData
            {
                Temp = Math.Round(current.GetProperty("temperature_2m").GetDouble()).ToString(),
                FeelsLike = Math.Round(current.GetProperty("apparent_temperature").GetDouble()).ToString(),
                Humidity = current.GetProperty("relative_humidity_2m").GetInt32().ToString(),
                Text = weatherText,
                WindDir = windDir,
                WindScale = windScale.ToString(),
                Vis = current.TryGetProperty("visibility", out var visProp) ? visProp.GetDouble().ToString() : "10",
            };

            var forecastData = new ForecastData
            {
                TempMax = Math.Round(daily.GetProperty("temperature_2m_max")[0].GetDouble()).ToString(),
                TempMin = Math.Round(daily.GetProperty("temperature_2m_min")[0].GetDouble()).ToString(),
            };

            return (nowData, forecastData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Open-Meteo 请求异常: {ex.Message}");
            return (null, null);
        }
    }

    static string AngleToDirection(double deg)
    {
        string[] dirs = { "北风", "东北风", "东风", "东南风", "南风", "西南风", "西风", "西北风" };
        int idx = ((int)Math.Round(deg / 45)) % 8;
        return dirs[idx];
    }

    static int MsToScale(double ms)
    {
        if (ms < 0.3) return 0;
        if (ms < 1.6) return 1;
        if (ms < 3.4) return 2;
        if (ms < 5.5) return 3;
        if (ms < 8.0) return 4;
        if (ms < 10.8) return 5;
        if (ms < 13.9) return 6;
        if (ms < 17.2) return 7;
        if (ms < 20.8) return 8;
        if (ms < 24.5) return 9;
        if (ms < 28.5) return 10;
        if (ms < 32.7) return 11;
        return 12;
    }

    // ========== 穿衣建议 ==========
    static string GetClothingAdvice(string tempStr, string humidityStr, string weatherText)
    {
        var advice = new List<string>();
        double temp = double.Parse(tempStr);
        int humidity = int.Parse(humidityStr);

        if (temp < -5) advice.Add("🧥 极寒！羽绒服+厚毛衣+围巾手套，全副武装！");
        else if (temp < 0) advice.Add("🧥 羽绒服/厚棉衣 + 保暖内衣，注意防寒");
        else if (temp < 10) advice.Add("🧥 大衣/厚外套 + 毛衣，保暖为主");
        else if (temp < 16) advice.Add("🧥 薄外套/卫衣/针织衫，洋葱式穿搭");
        else if (temp < 22) advice.Add("👕 长袖T恤/衬衫，早晚可加薄外套");
        else if (temp < 26) advice.Add("👕 短袖/薄衬衫，舒适清爽");
        else if (temp < 30) advice.Add("🩳 短袖短裤，注意防晒和补水");
        else advice.Add("🩳 清凉装+遮阳帽，多喝水防中暑！");

        if (humidity > 85) advice.Add("💧 湿度很高，体感闷热，注意通风除湿");
        else if (humidity > 70) advice.Add("💧 湿度较大，体感偏闷，建议穿透气衣物");
        else if (humidity < 30) advice.Add("💧 空气干燥，记得涂护手霜、多喝水");

        string[] rainKeywords = { "雨", "雪", "霰", "雹", "冻雨" };
        if (rainKeywords.Any(k => weatherText.Contains(k)))
            advice.Add("☔ 今天有降水，出门记得带伞哦！");

        string[] coldKeywords = { "冷", "寒", "冻", "冰", "霜" };
        if (coldKeywords.Any(k => weatherText.Contains(k)))
            advice.Add("❄️ 天气寒冷，多喝热水，注意保暖");

        string[] hotKeywords = { "热", "暑", "高温", "酷热" };
        if (hotKeywords.Any(k => weatherText.Contains(k)))
            advice.Add("🌞 天气炎热，注意防晒、遮阳、补水");

        string[] windKeywords = { "大风", "狂风", "飓风", "台风" };
        if (windKeywords.Any(k => weatherText.Contains(k)))
            advice.Add("🌬 风力较大，注意防风，远离广告牌");

        return string.Join("\n", advice);
    }

    // ========== 恋爱天数 ==========
    static int? GetLoveDays()
    {
        if (string.IsNullOrEmpty(START_DATE)) return null;
        try
        {
            var start = DateTime.ParseExact(START_DATE, "yyyy-MM-dd", null);
            return (DateTime.Now - start).Days;
        }
        catch
        {
            return null;
        }
    }

    // ========== 构建消息 ==========
    static (string title, string content) BuildMessage(WeatherData now, ForecastData? forecast)
    {
        string quote = LOVE_QUOTES[rnd.Next(LOVE_QUOTES.Length)];
        string clothing = GetClothingAdvice(now.Temp, now.Humidity, now.Text);
        int? loveDays = GetLoveDays();
        string loveInfo = loveDays.HasValue ? $"\n💕 今天是我们的第 {loveDays.Value} 天" : "";

        string todayStr = DateTime.Now.ToString("yyyy年MM月dd日");
        string[] weekdays = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
        string weekday = weekdays[(int)DateTime.Now.DayOfWeek];

        string title = $"☀️ 早安宝贝 | {CITY_NAME} {now.Text} {now.Temp}℃";

        string content = $@"📅 {todayStr} {weekday}{loveInfo}

━━━━━━━━━━━━━━━

🌤 今日天气
• 天气状况：{now.Text}
• 实时温度：{now.Temp}℃（体感 {now.FeelsLike}℃）
• 今日气温：{(forecast?.TempMin ?? "--")}℃ ~ {(forecast?.TempMax ?? "--")}℃
• 湿度：{now.Humidity}%
• 风向风力：{now.WindDir} {now.WindScale}级
• 能见度：{now.Vis}km

━━━━━━━━━━━━━━━

👗 穿衣建议
{clothing}

━━━━━━━━━━━━━━━

💌 今日情话
{quote}

━━━━━━━━━━━━━━━

✨ 今天也要元气满满哦，爱你～ 💖
";

        return (title, content);
    }

    // ========== PushPlus 推送 ==========
    static async Task PushMessageAsync(string title, string content)
    {
        var payload = new
        {
            token = PUSH_TOKEN,
            title = title,
            content = content,
            template = "txt"
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var resp = await http.PostAsync(
                "http://www.pushplus.plus/send",
                new StringContent(json, Encoding.UTF8, "application/json")
            );
            var result = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(result);
            if (doc.RootElement.TryGetProperty("code", out var codeProp) && codeProp.GetInt32() == 200)
            {
                Console.WriteLine("✅ 推送成功！消息已发送到微信");
            }
            else
            {
                Console.WriteLine($"❌ 推送失败: {result}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 推送异常: {ex.Message}");
        }
    }
}

// ========== 数据模型 ==========
class WeatherData
{
    public string Temp { get; set; } = "";
    public string FeelsLike { get; set; } = "";
    public string Humidity { get; set; } = "";
    public string Text { get; set; } = "";
    public string WindDir { get; set; } = "";
    public string WindScale { get; set; } = "";
    public string Vis { get; set; } = "";
}

class ForecastData
{
    public string TempMax { get; set; } = "";
    public string TempMin { get; set; } = "";
}
