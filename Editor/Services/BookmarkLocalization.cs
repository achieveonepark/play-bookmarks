using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Achieve.PlayBookmarks.Editor
{
    public static class BookmarkLocalization
    {
        private const string PreferenceKey = "Achieve.Editor.Language";
        private static readonly Dictionary<string, string> KoreanText = new Dictionary<string, string>
        {
            { "Apply", "적용" },
            { "Revert", "되돌리기" },
            { "Enabled", "활성화" },
            { "Timeout Seconds", "제한 시간 (초)" },
            { "Language", "언어" },
            { "Notes", "메모" },
            { "Position", "위치" },
            { "Rotation", "회전" },
            { "Target Id", "대상 ID" },
            { "Action Id", "액션 ID" },
            { "Payload Json", "JSON 인자" },
            { "Seconds", "대기 시간 (초)" },
            { "Start scene", "시작 씬" },
            { "Seed", "시드" },
            { "Use Random Seed", "랜덤 시드 사용" },
            { "Random Seed", "랜덤 시드" },
            { "Description", "설명" },
            { "Ready Signal Id", "준비 신호 ID" },
            { "Ready Timeout Seconds", "준비 제한 시간 (초)" },
            { "Missing Type", "누락된 타입" },
            { "Original Json", "원본 JSON" },
            { "Record", "기록" },
            { "Info", "정보" },
            { "Warning", "경고" },
            { "Error", "오류" },
            { "Play Bookmarks", "플레이 북마크" },
            { "Test starting conditions", "테스트 시작 조건" },
            { "Bookmarks", "북마크" },
            { "No bookmarks yet.\nCreate one to get started.", "북마크가 없습니다.\n새 북마크를 만들어 시작하세요." },
            { "+ New bookmark", "+ 새 북마크" },
            { "Choose a bookmark", "북마크 선택" },
            { "Run bookmark", "북마크 실행" },
            { "Cancel setup", "설정 취소" },
            { "Select a .pbm file to edit its starting conditions.", ".pbm 파일을 선택해 시작 조건을 편집하세요." },
            { "Create a .pbm file or select one from the list.", ".pbm 파일을 만들거나 목록에서 선택하세요." },
            { "No bookmark selected", "선택한 북마크가 없습니다" },
            { "Create a bookmark to configure its scene and setup steps.", "북마크를 만들어 시작 씬과 설정 단계를 지정하세요." },
            { "Choose a start scene", "시작 씬을 지정하세요" },
            { "No bookmark has run in this session.", "이 세션에서 아직 실행한 북마크가 없습니다." },
            { "Starting conditions", "시작 조건" },
            { "Set Unity random seed", "Unity 랜덤 시드 지정" },
            { "Ready signal (optional)", "준비 신호 (선택)" },
            { "Ready timeout · seconds", "준비 제한 시간 (초)" },
            { "Setup steps", "설정 단계" },
            { "+ Add step", "+ 단계 추가" },
            { "Add a step to prepare the situation you want to test.", "테스트할 상황을 준비하는 단계를 추가하세요." },
            { "Missing step type", "단계 타입을 찾을 수 없습니다" },
            { "Move up", "위로 이동" },
            { "Move down", "아래로 이동" },
            { "Remove step", "단계 삭제" },
            { "Move Target Step", "대상 이동" },
            { "Set Active Step", "활성 상태 변경" },
            { "Wait Step", "시간 대기" },
            { "Wait For Target Step", "대상 생성 대기" },
            { "Invoke Action Step", "게임 액션 실행" },
            { "IDLE", "대기" },
            { "Preparing", "준비 중" },
            { "WaitingForReady", "준비 신호 대기" },
            { "Running", "실행 중" },
            { "Succeeded", "성공" },
            { "Failed", "실패" },
            { "Cancelled", "취소" },
            { "Pending", "예정" },
            { "Skipped", "건너뜀" },
            { "Preparing scene", "씬 준비 중" },
            { "Setup cancelled", "설정 취소됨" },
            { "Ready to play", "플레이 준비 완료" },
            { "Target ID is empty.", "대상 ID가 비어 있습니다." },
            { "Action ID is empty.", "액션 ID가 비어 있습니다." },
            { "Ready timeout must be a positive finite number.", "준비 제한 시간은 유한한 양수여야 합니다." },
            { "Steps list is missing.", "단계 목록이 없습니다." },
            { "timeout must be a positive finite number.", "제한 시간은 유한한 양수여야 합니다." },
            { "Waiting for ready signal", "준비 신호 대기 중" },
            { "Cancelled before scene setup", "씬 설정 전에 취소됨" },
            { "New bookmark", "새 북마크" },
            { "Choose a location for your bookmark file.", "북마크 파일을 저장할 위치를 선택하세요." }
        };
        private sealed class TextState { public string original; public string displayed; }
        private sealed class ElementState { public readonly Dictionary<string, TextState> values = new Dictionary<string, TextState>(); }
        private sealed class Schedule { public IVisualElementScheduledItem item; }
        private static readonly ConditionalWeakTable<VisualElement, ElementState> Originals = new ConditionalWeakTable<VisualElement, ElementState>();
        private static readonly ConditionalWeakTable<VisualElement, Schedule> Schedules = new ConditionalWeakTable<VisualElement, Schedule>();
        public static bool IsKorean => EditorPrefs.GetString(PreferenceKey,
            Application.systemLanguage == SystemLanguage.Korean ? "ko" : "en") == "ko";
        public static void SetLanguage(bool korean) => EditorPrefs.SetString(PreferenceKey, korean ? "ko" : "en");
        public static void SetText(TextElement element, string english)
        {
            var state = Originals.GetValue(element, _ => new ElementState());
            var translated = Text(english);
            state.values["text"] = new TextState { original = english, displayed = translated };
            element.text = translated;
        }
        public static string Text(string english)
        {
            if (!IsKorean || string.IsNullOrEmpty(english)) return english;
            if (KoreanText.TryGetValue(english, out var translated)) return translated;
            if (english.Contains("\n")) return string.Join("\n", english.Split('\n').Select(Text));
            var step = Regex.Match(english, @"^Step (\d+): (.*)$");
            if (step.Success) return $"단계 {step.Groups[1]}: {Text(step.Groups[2].Value)}";
            step = Regex.Match(english, @"^Step (\d+)/(\d+): (.*)$");
            if (step.Success) return $"단계 {step.Groups[1]}/{step.Groups[2]}: {step.Groups[3]}";
            step = Regex.Match(english, @"^Waiting for '(.*)'$");
            if (step.Success) return $"준비 신호 '{step.Groups[1]}' 대기 중";
            step = Regex.Match(english, @"^Ready signal '(.*)' timed out\.$");
            if (step.Success) return $"준비 신호 '{step.Groups[1]}'의 제한 시간이 초과됐습니다.";
            var match = Regex.Match(english, @"^([\d,]+) shown · ([\d,]+) captured · ([\d,]+) dropped$");
            if (match.Success) return $"표시 {match.Groups[1]} · 수집 {match.Groups[2]} · 제외 {match.Groups[3]}";
            match = Regex.Match(english, @"^([\d,]+) events · ([\d,]+) dropped$");
            if (match.Success) return $"이벤트 {match.Groups[1]}개 · 제외 {match.Groups[2]}개";
            match = Regex.Match(english, @"^RECORDED EFFECTS  ·  (\d+)$");
            if (match.Success) return $"기록된 결과 · {match.Groups[1]}개";
            match = Regex.Match(english, @"^([\d,]+) more (events in the trace window|effects in the event list)\.$");
            if (match.Success) return $"나머지 {match.Groups[1]}개는 추적 창에서 확인하세요.";
            return english;
        }
        public static ToolbarMenu CreateSelector()
        {
            var menu = new ToolbarMenu { name = "achieve-language", text = IsKorean ? "한국어" : "English", tooltip = "한국어 / English" };
            menu.menu.AppendAction("한국어", _ => SetLanguage(true), _ => IsKorean ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            menu.menu.AppendAction("English", _ => SetLanguage(false), _ => IsKorean ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Checked);
            return menu;
        }
        public static void Install(VisualElement root)
        {
            if (Schedules.TryGetValue(root, out var previous)) previous.item.Pause();
            Schedules.Remove(root);
            var item = root.schedule.Execute(() => Apply(root)).Every(200);
            Schedules.Add(root, new Schedule { item = item });
            Apply(root);
        }
        public static void Apply(VisualElement root)
        {
            var pending = new Stack<VisualElement>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var element = pending.Pop();
                foreach (var child in element.Children().ToArray()) pending.Push(child);
                if (element.ClassListContains("achieve-data")) continue;
                if (element is ToolbarMenu menu && menu.name == "achieve-language")
                {
                    menu.text = IsKorean ? "한국어" : "English";
                    continue;
                }
                Transform(element, "tooltip", () => element.tooltip, text => element.tooltip = text);
                // Input values, event payloads and identifiers are deliberately never translated.
                if (element is Label label) Transform(element, "text", () => label.text, text => label.text = text);
                else if (element is Button button) Transform(element, "text", () => button.text, text => button.text = text);
                else if (element is Foldout foldout) Transform(element, "text", () => foldout.text, text => foldout.text = text);
                var property = element.GetType().GetProperty("label");
                if (property != null && property.PropertyType == typeof(string) && property.CanRead && property.CanWrite)
                    Transform(element, "label", () => (string)property.GetValue(element), text => property.SetValue(element, text));
            }
        }
        private static void Transform(VisualElement element, string key, Func<string> read, Action<string> write)
        {
            var current = read();
            if (string.IsNullOrEmpty(current)) return;
            var state = Originals.GetValue(element, _ => new ElementState());
            if (!state.values.TryGetValue(key, out var textState))
            {
                textState = new TextState { original = EnglishOriginal(current) };
                state.values.Add(key, textState);
            }
            else if (current != textState.displayed && current != textState.original) textState.original = EnglishOriginal(current);
            var translated = Text(textState.original);
            textState.displayed = translated;
            if (current != translated) write(translated);
        }
        private static string EnglishOriginal(string current)
        {
            foreach (var entry in KoreanText) if (entry.Value == current) return entry.Key;
            return current;
        }
    }
}
