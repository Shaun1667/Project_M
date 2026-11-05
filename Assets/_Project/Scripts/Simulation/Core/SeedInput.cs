using System.Globalization;

namespace ProjectM.Core
{
    /// <summary>
    /// 시드 입력 (3-27, 결정 2026-11-05): 모드 고르는 화면의 입력 칸 글 → 판 시드. 비면 무작위(false).
    /// 정수(결과 화면에 보이는 값, 음수 포함)면 그 값 그대로, 그 밖의 글은 FNV-1a 64비트 해시 — 같은 글 = 같은 시드 = 같은 층 (규칙 7).
    /// </summary>
    public static class SeedInput
    {
        public const int MaxLength = 24;

        public static bool TryParse(string text, out ulong seed)
        {
            seed = 0;
            if (text == null) return false;
            text = text.Trim();
            if (text.Length == 0) return false;
            if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var signed)) seed = unchecked((ulong)signed);
            else if (ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var unsigned)) seed = unsigned;
            else seed = Hash(text);
            if (seed == 0) seed = Hash(text); // 0은 "고정 시드 없음"과 겹치지 않게
            return true;
        }

        /// <summary>FNV-1a 64비트 (문자 UTF-16 단위). 플랫폼 · 실행마다 같다 (string.GetHashCode 금지).</summary>
        public static ulong Hash(string text)
        {
            ulong h = 14695981039346656037UL;
            foreach (char c in text)
            {
                h ^= (byte)(c & 0xFF);
                h *= 1099511628211UL;
                h ^= (byte)(c >> 8);
                h *= 1099511628211UL;
            }
            return h == 0 ? 1UL : h;
        }
    }
}
