﻿
namespace Notepads.Core
{
    using Notepads.Controls.TextEditor;
    using Notepads.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.UI.Xaml.Input;

    // INotepadsCore handles Tabs and TextEditor life cycle
    public interface INotepadsCore
    {
        event EventHandler<TextEditor> TextEditorLoaded;

        event EventHandler<TextEditor> TextEditorUnloaded;

        event EventHandler<TextEditor> TextEditorEditorModificationStateChanged;

        event EventHandler<TextEditor> TextEditorFileModificationStateChanged;

        event EventHandler<TextEditor> TextEditorSaved;

        event EventHandler<TextEditor> TextEditorClosingWithUnsavedContent;

        event EventHandler<TextEditor> TextEditorSelectionChanged;

        event EventHandler<TextEditor> TextEditorEncodingChanged;

        event EventHandler<TextEditor> TextEditorLineEndingChanged;

        event EventHandler<TextEditor> TextEditorModeChanged;

        event EventHandler<IReadOnlyList<IStorageItem>> StorageItemsDropped;

        event KeyEventHandler TextEditorKeyDown;

        TextEditor OpenNewTextEditor(Guid? id = null, int atIndex = -1);

        Task<TextEditor> OpenNewTextEditor(StorageFile file, bool ignoreFileSizeLimit, Guid? id = null, int atIndex = -1);

        TextEditor OpenNewTextEditor(
            Guid id,
            string text,
            StorageFile file,
            long dateModifiedFileTime,
            Encoding encoding,
            LineEnding lineEnding,
            bool isModified,
            int atIndex = -1);

        Task SaveContentToFileAndUpdateEditorState(TextEditor textEditor, StorageFile file);

        void DeleteTextEditor(TextEditor textEditor);

        int GetNumberOfOpenedTextEditors();

        bool TryGetSharingContent(TextEditor textEditor, out string title, out string content);

        bool HaveUnsavedTextEditor();

        void ChangeLineEnding(TextEditor textEditor, LineEnding lineEnding);

        void ChangeEncoding(TextEditor textEditor, Encoding encoding);

        void SwitchTo(bool next);

        void SwitchTo(TextEditor textEditor);

        TextEditor GetSelectedTextEditor();

        TextEditor GetTextEditor(string editingFilePath);

        TextEditor[] GetAllTextEditors();

        void FocusOnTextEditor(TextEditor textEditor);

        void FocusOnSelectedTextEditor();

        void CloseTextEditor(TextEditor textEditor);
    }
}
