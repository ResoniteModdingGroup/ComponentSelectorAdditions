using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ComponentSelectorAdditions;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using MonkeyLoader.Patching;
using MonkeyLoader.Resonite;
using MonkeyLoader.Resonite.UI;
using Zio;

namespace ComponentSelectorSearch
{
    [HarmonyPatch(typeof(ComponentSelector), "BuildUI")]
    [HarmonyPatchCategory(nameof(ComponentSelectorSearch))]
    internal sealed class ComponentSelectorSearch : ConfiguredResoniteMonkey<ComponentSelectorSearch, SearchConfig>
    {
        private const string SearchPath = "/Search/";

        private static readonly ConditionalWeakTable<ComponentSelector, AttacherDetails> _selectorDetails = new();

        protected override IEnumerable<IFeaturePatch> GetFeaturePatches() => Enumerable.Empty<IFeaturePatch>();

        private static void AddHoverButtons(UIBuilder builder, ComponentSelector selector, string path, string search, IEnumerable<ComponentResult> results, KeyCounter<string> keyCounter, string? group)
        {
            var groupSet = new HashSet<string>();

            foreach (var result in results)
            {
                if (group != null && result.Group != group)
                    continue;

                Button button;
                var name = result.Type.GetNiceName();

                if (group == null && result.HasGroup && keyCounter[result.Group] > 1)
                {
                    if (!groupSet.Add(result.Group))
                        continue;

                    button = builder.Button(result.Group.Split('.').Last(), RadiantUI_Constants.Sub.PURPLE, selector.OpenGroupPressed, $"{path}{SearchPath}{search}:{result.Group}", .35f);
                }
                else
                {
                    var fullName = group != null ? $"{result.Type.FullName}?{group}" : result.Type.FullName;

                    button = result.Type.IsGenericTypeDefinition ?
                            builder.Button(name, RadiantUI_Constants.Sub.GREEN, selector.OpenGenericTypesPressed, path + SearchPath + search + "/" + fullName, .35f)
                            : builder.Button(name, RadiantUI_Constants.Sub.CYAN, selector.OnAddComponentPressed, result.Type.FullName, .35f);
                }

                button.Label.ParseRichText.Value = false;

                var booleanDriver = button.Slot.AttachComponent<BooleanValueDriver<string>>();
                booleanDriver.TargetField.Target = button.LabelTextField;
                booleanDriver.State.DriveFrom(button.IsHovering);

                booleanDriver.TrueValue.Value = GetPrettyPath(result.Category.GetPath()) + " > " + name;
                booleanDriver.FalseValue.Value = name;
            }
        }

        private static void AddPermanentButtons(UIBuilder builder, ComponentSelector selector, string path, string search, IEnumerable<ComponentResult> results, KeyCounter<string> keyCounter, string? group)
        {
            builder.PushStyle();
            builder.Style.MinHeight = 48;
            var root = builder.CurrentRect;

            var groupSet = new HashSet<string>();

            foreach (var result in results)
            {
                if (group != null && result.Group != group)
                    continue;

                Button button;

                if (group == null && result.HasGroup && keyCounter[result.Group] > 1)
                {
                    if (!groupSet.Add(result.Group))
                        continue;

                    button = builder.Button(result.Group.Split('.').Last(), RadiantUI_Constants.Sub.PURPLE, selector.OpenGroupPressed, $"{path}{SearchPath}{search}:{result.Group}", .35f);
                    continue;
                }
                else
                {
                    var name = result.Type.GetNiceName();
                    var fullName = group != null ? $"{result.Type.FullName}?{group}" : result.Type.FullName;

                    button = result.Type.IsGenericTypeDefinition ?
                            builder.Button(name, RadiantUI_Constants.Sub.GREEN, selector.OpenGenericTypesPressed, path + SearchPath + search + "/" + fullName, .35f)
                            : builder.Button(name, RadiantUI_Constants.Sub.CYAN, selector.OnAddComponentPressed, result.Type.FullName, .35f);
                }

                var buttonLabel = button.Label;
                buttonLabel.ParseRichText.Value = false;
                builder.NestInto(button.RectTransform);

                var panel = builder.Panel();
                panel.OffsetMin.Value = buttonLabel.RectTransform.OffsetMin;
                panel.OffsetMax.Value = buttonLabel.RectTransform.OffsetMax;

                builder.HorizontalHeader(16, out var header, out var content);

                buttonLabel.Slot.Parent = content.Slot;
                buttonLabel.RectTransform.OffsetMin.Value = new(16, 0);
                buttonLabel.RectTransform.OffsetMax.Value = float2.Zero;

                builder.NestInto(header);
                var text = builder.Text(GetPrettyPath(result.Category.GetPath()) + " >", parseRTF: false);
                text.Color.Value = RadiantUI_Constants.Neutrals.MIDLIGHT;

                builder.NestOut();
                builder.NestOut();
                builder.NestOut();
            }

            builder.PopStyle();
        }

        private static void AddSearchbar(ComponentSelector attacher, AttacherDetails details, string? search)
        {
            var uiRoot = attacher._uiRoot.Target;

            var builder = SetupUIBuilder(uiRoot.Parent.Parent);

            builder.HorizontalHeader(56, out var header, out var content);
            uiRoot.Parent.Parent = content.Slot;

            header.OffsetMin.Value += new float2(8, 8);
            header.OffsetMax.Value += new float2(-8, 0);

            builder.NestInto(header);
            builder.VerticalFooter(56, out var footer, out content);
            footer.OffsetMin.Value += new float2(8, 0);

            builder.NestInto(content);
            details.SearchBar = builder.TextField(search, parseRTF: false);
            details.Text.NullContent.AssignLocaleString($"{Mod.Id}.Search".AsLocaleKey());
            details.Editor.FinishHandling.Value = TextEditor.FinishAction.NullOnWhitespace;
            details.Text.Content.OnValueChange += MakeBuildUICall(attacher, details);

            builder.NestInto(footer);
            builder.Style.ButtonTextAlignment = Alignment.MiddleCenter;
            builder.LocalActionButton("∅", _ => details.Text.Content.Value = null);
        }

        [HarmonyPrefix]
        private static bool BuildUIPrefix(ComponentSelector __instance, ref string path, bool genericType, string? group)
        {
            if (!_selectorDetails.TryGetValue(__instance, out var details))
            {
                details = new AttacherDetails(__instance);
                _selectorDetails.Add(__instance, details);
            }

            if (genericType)
            {
                if (details.HasSearchBar)
                {
                    ClearSearchbar(__instance);
                    details.SearchBar = null;
                }

                return true;
            }

            var componentLibrary = PickComponentLibrary(ref path, out var search);
            details.LastPath = path;

            if (details.SearchBar == null)
                AddSearchbar(__instance, details, search);

            if (search == null && !details.Editor.IsEditing)
                details.Text.Content.Value = null;

            if (string.IsNullOrWhiteSpace(search) || (search!.Length < 3 && path.Length < 2))
                return true;

            var builder = SetupUIBuilder(__instance._uiRoot);
            builder.Root.DestroyChildren();

            if (group != null)
                builder.Button("ComponentSelector.Back".AsLocaleKey(), RadiantUI_Constants.BUTTON_COLOR, __instance.OnOpenCategoryPressed, path + SearchPath + search, .35f);

            foreach (var subCategory in SearchCategories(componentLibrary, search))
            {
                var categoryPath = subCategory.GetPath();
                builder.Button(GetPrettyPath(categoryPath) + " >", RadiantUI_Constants.Sub.YELLOW, details.OnOpenCategoryPressed, categoryPath, 0.35f).Label.ParseRichText.Value = false;
            }

            var typeResults = SearchTypes(componentLibrary, search);

            var keyCounter = new KeyCounter<string>();

            if (group == null)
            {
                foreach (var typeResult in typeResults.Where(typeResult => typeResult.HasGroup))
                    keyCounter.Increment(typeResult.Group!);
            }

            if (ConfigSection.AlwaysShowFullPath)
                AddPermanentButtons(builder, __instance, path, search, typeResults, keyCounter, group);
            else
                AddHoverButtons(builder, __instance, path, search, typeResults, keyCounter, group);

            builder.Button("General.Cancel".AsLocaleKey(), RadiantUI_Constants.Sub.RED, details.OnCancelPressed, 0.35f).Slot.OrderOffset = 1000000;

            return false;
        }

        private static void ClearSearchbar(ComponentSelector attacher)
        {
            var contentRoot = attacher._uiRoot.Target.Parent;

            contentRoot.Parent = contentRoot.Parent.Parent;
            contentRoot.Parent.DestroyChildren(filter: slot => slot != contentRoot);
        }

        private static string GetPrettyPath(string path)
            => path[1..].Replace("/", " > ");

        private static SyncFieldEvent<string> MakeBuildUICall(ComponentSelector selector, AttacherDetails details)
        {
            return field =>
            {
                details.LastResultUpdate.Cancel();
                details.LastResultUpdate = new CancellationTokenSource();
                var token = details.LastResultUpdate.Token;

                selector.StartTask(async () =>
                {
                    if (ConfigSection.SearchRefreshDelay > 0)
                    {
                        await default(ToBackground);
                        await Task.Delay(ConfigSection.SearchRefreshDelay);
                        await default(NextUpdate);
                    }

                    // Only refresh UI with search results if there was no further update immediately following it
                    if (token.IsCancellationRequested || selector.IsDestroyed)
                        return;

                    selector.BuildUI(details.LastPath + SearchPath + field.Value, false);
                });
            };
        }

        private static CategoryNode<Type> PickComponentLibrary(ref string path, out string? search)
        {
            search = null;

            if (string.IsNullOrEmpty(path) || path == "/")
            {
                path = "";
                return WorkerInitializer.ComponentLibrary;
            }

            path = path.Replace('\\', '/');

            var searchIndex = path.IndexOf(SearchPath);
            if (searchIndex >= 0)
            {
                if (searchIndex + SearchPath.Length < path.Length)
                    search = path[(searchIndex + SearchPath.Length)..].Replace(" ", "");

                path = path.Remove(searchIndex);
            }

            var categoryNode = WorkerInitializer.ComponentLibrary.GetSubcategory(path);
            if (categoryNode == null)
            {
                path = "";
                return WorkerInitializer.ComponentLibrary;
            }

            return categoryNode;
        }

        private static IEnumerable<CategoryNode<Type>> SearchCategories(CategoryNode<Type> root, string? search = null)
        {
            var returnAll = search is null;
            var queue = new Queue<CategoryNode<Type>>();

            foreach (var subCategory in root.Subcategories)
                queue.Enqueue(subCategory);

            while (queue.Count > 0)
            {
                var category = queue.Dequeue();

                if (ConfigSection.HasExcludedCategory(category.GetPath()))
                    continue;

                if (returnAll || SearchContains(category.Name, search!))
                    yield return category;

                foreach (var subCategory in category.Subcategories)
                    queue.Enqueue(subCategory);
            }
        }

        private static bool SearchContains(string haystack, string needle)
            => CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;

        private static IEnumerable<ComponentResult> SearchTypes(CategoryNode<Type> root, string search)
            => root.Elements
                .Where(type => SearchContains(type.Name, search))
                .Select(type => new ComponentResult(root, type))
                .Concat(
                    SearchCategories(root)
                    .SelectMany(category =>
                        category.Elements
                        .Where(type => SearchContains(type.Name, search))
                        .Select(type => new ComponentResult(category, type))))
                .OrderBy(result => result.Type.Name);

        private static UIBuilder SetupUIBuilder(Slot root)
        {
            var builder = new UIBuilder(root);
            RadiantUI_Constants.SetupEditorStyle(builder, extraPadding: true);

            builder.Style.TextAlignment = Alignment.MiddleLeft;
            builder.Style.ButtonTextAlignment = Alignment.MiddleLeft;
            builder.Style.MinHeight = 32;

            return builder;
        }
    }
}