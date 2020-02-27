﻿namespace Notepads.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Notepads.Controls.TextEditor;
    using Notepads.Controls.Print;
    using Windows.Graphics.Printing;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Printing;
    using Windows.ApplicationModel.Core;
    using Windows.UI.Xaml.Media;
    using Windows.Graphics.Printing.OptionDetails;

    public static class PrintService
    {
        private static string _headerText = string.Empty;

        private static string _footerText = string.Empty;

        private static string _filename;

        public static async Task Print(ITextEditor textEditor)
        {
            // Initialize print content
            PreparePrintContent(textEditor);
            _filename = textEditor.EditingFileName;

            if (PrintManager.IsSupported() && !string.IsNullOrEmpty(textEditor.GetText()))
            {
                // Show print UI
                await ShowPrintUIAsync();
            }
            else
            {
                // Printing is not supported on this device
                NotificationCenter.Instance.PostNotification("Printing is not supported on this device", 1500);
            }
        }

        public static async Task PrintAll(ITextEditor[] textEditors)
        {
            return;
        }

        /// <summary>
        /// The percent of app's margin width, content is set at 85% (0.85) of the area's width
        /// </summary>
        private static double _applicationContentMarginLeft = 0.075;

        /// <summary>
        /// The percent of app's margin height, content is set at 94% (0.94) of tha area's height
        /// </summary>
        private static double _applicationContentMarginTop = 0.03;

        /// <summary>
        /// PrintDocument is used to prepare the pages for printing.
        /// Prepare the pages to print in the handlers for the Paginate, GetPreviewPage, and AddPages events.
        /// </summary>
        private static PrintDocument _printDocument;

        /// <summary>
        /// Marker interface for document source
        /// </summary>
        private static IPrintDocumentSource _printDocumentSource;

        /// <summary>
        /// A list of UIElements used to store the print preview pages.  This gives easy access
        /// to any desired preview page.
        /// </summary>
        private static List<UIElement> _printPreviewPages;

        /// <summary>
        /// Event callback which is called after print preview pages are generated.
        /// </summary>
        private static event EventHandler PreviewPagesCreated;

        /// <summary>
        /// First page in the printing-content series
        /// From this "virtual sized" paged content is split(text is flowing) to "printing pages"
        /// </summary>
        private static FrameworkElement _firstPage;

        /// <summary>
        ///  A reference back to the source page used to access XAML elements on the source page
        /// </summary>
        private static Page _sourcePage;

        /// <summary>
        ///  A hidden canvas used to hold pages we wish to print
        /// </summary>
        private static Canvas PrintCanvas
        {
            get
            {
                return _sourcePage.FindName("PrintCanvas") as Canvas;
            }
        }

        /// <summary>
        /// This function registers the app for printing with Windows and sets up the necessary event handlers for the print process.
        /// </summary>
        public static void RegisterForPrinting(Page sourcePage)
        {
            _sourcePage = sourcePage;
            _printPreviewPages = new List<UIElement>();

            _printDocument = new PrintDocument();
            _printDocumentSource = _printDocument.DocumentSource;
            _printDocument.Paginate += CreatePrintPreviewPages;
            _printDocument.GetPreviewPage += GetPrintPreviewPage;
            _printDocument.AddPages += AddPrintPages;

            PrintManager printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested += PrintTaskRequested;
        }

        /// <summary>
        /// This function unregisters the app for printing with Windows.
        /// </summary>
        public static void UnregisterForPrinting()
        {
            if (_printDocument == null)
            {
                return;
            }

            _printDocument.Paginate -= CreatePrintPreviewPages;
            _printDocument.GetPreviewPage -= GetPrintPreviewPage;
            _printDocument.AddPages -= AddPrintPages;

            // Remove the handler for printing initialization.
            PrintManager printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested -= PrintTaskRequested;

            PrintCanvas.Children.Clear();
        }

        private static async Task ShowPrintUIAsync()
        {
            // Catch and print out any errors reported
            try
            {
                await PrintManager.ShowPrintUIAsync();
            }
            catch (Exception e)
            {
                NotificationCenter.Instance.PostNotification("Error printing: " + e.Message + ", hr=" + e.HResult, 1500);
            }
        }

        /// <summary>
        /// Method that will generate print content for the scenario
        /// It will create the first page from which content will flow
        /// </summary>
        /// <param name="page">The page to print</param>
        private static void PreparePrintContent(ITextEditor textEditor)
        {
            // Clear the cache of preview pages
            _printPreviewPages.Clear();

            // Clear the print canvas of preview pages
            PrintCanvas.Children.Clear();

            _firstPage = new PrintPageFormat(textEditor.GetText(),
                new FontFamily(EditorSettingsService.EditorFontFamily),
                EditorSettingsService.EditorFontSize,
                _headerText,
                _footerText);

            // Add the (newly created) page to the print canvas which is part of the visual tree and force it to go
            // through layout so that the linked containers correctly distribute the content inside them.
            PrintCanvas.Children.Add(_firstPage);
            PrintCanvas.InvalidateMeasure();
            PrintCanvas.UpdateLayout();
        }

        /// <summary>
        /// This is the event handler for PrintManager.PrintTaskRequested.
        /// </summary>
        /// <param name="sender">PrintManager</param>
        /// <param name="e">PrintTaskRequestedEventArgs </param>
        private static void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
        {
            PrintTask printTask = null;
            printTask = e.Request.CreatePrintTask("Notepads", sourceRequestedArgs =>
            {
                var deferral = sourceRequestedArgs.GetDeferral();
                PrintTaskOptionDetails printDetailedOptions = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
                IList<string> displayedOptions = printTask.Options.DisplayedOptions;

                // Choose the printer options to be shown.
                // The order in which the options are appended determines the order in which they appear in the UI
                displayedOptions.Clear();
                displayedOptions.Add(StandardPrintTaskOptions.Copies);
                displayedOptions.Add(StandardPrintTaskOptions.Orientation);
                displayedOptions.Add(StandardPrintTaskOptions.MediaSize);

                // Add Header and Footer text options
                PrintCustomTextOptionDetails headerText = printDetailedOptions.CreateTextOption("HeaderText", "Header");
                PrintCustomTextOptionDetails footerText = printDetailedOptions.CreateTextOption("FooterText", "Footer");
                headerText.TrySetValue(_headerText);
                footerText.TrySetValue(_footerText);
                displayedOptions.Add("HeaderText");
                displayedOptions.Add("FooterText");

                // Add Margin setting in % options
                PrintCustomTextOptionDetails leftMargin = printDetailedOptions.CreateTextOption("LeftMargin", "Horizontal Margin (in %)");
                PrintCustomTextOptionDetails topMargin = printDetailedOptions.CreateTextOption("TopMargin", "Vertical Margin (in %)");
                leftMargin.Description = "In % of paper width";
                topMargin.Description = "In % of paper width";
                leftMargin.TrySetValue(Math.Round(100 * _applicationContentMarginLeft, 1).ToString());
                topMargin.TrySetValue(Math.Round(100 * _applicationContentMarginTop, 1).ToString());
                displayedOptions.Add("LeftMargin");
                displayedOptions.Add("TopMargin");

                displayedOptions.Add(StandardPrintTaskOptions.Collation);
                displayedOptions.Add(StandardPrintTaskOptions.Duplex);
                displayedOptions.Add(StandardPrintTaskOptions.CustomPageRanges);
                displayedOptions.Add(StandardPrintTaskOptions.NUp);
                displayedOptions.Add(StandardPrintTaskOptions.MediaType);
                displayedOptions.Add(StandardPrintTaskOptions.InputBin);
                displayedOptions.Add(StandardPrintTaskOptions.Bordering);
                displayedOptions.Add(StandardPrintTaskOptions.ColorMode);
                displayedOptions.Add(StandardPrintTaskOptions.PrintQuality);
                displayedOptions.Add(StandardPrintTaskOptions.HolePunch);
                displayedOptions.Add(StandardPrintTaskOptions.Staple);

                // Preset the default value of the printer option
                printTask.Options.MediaSize = PrintMediaSize.Default;

                printDetailedOptions.OptionChanged += PrintDetailedOptions_OptionChanged;

                // Print Task event handler is invoked when the print job is completed.
                printTask.Completed += async (s, args) =>
                {
                    // Notify the user when the print operation fails.
                    if (args.Completion == PrintTaskCompletion.Failed)
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            NotificationCenter.Instance.PostNotification("Failed to print", 1500);
                        });
                    }
                };

                sourceRequestedArgs.SetSource(_printDocumentSource);

                deferral.Complete();
            });
        }

        private static async void PrintDetailedOptions_OptionChanged(PrintTaskOptionDetails sender, PrintTaskOptionChangedEventArgs args)
        {
            bool invalidatePreview = false;

            string optionId = args.OptionId as string;
            if (string.IsNullOrEmpty(optionId))
            {
                return;
            }

            if (optionId == "HeaderText")
            {
                PrintCustomTextOptionDetails headerText = (PrintCustomTextOptionDetails)sender.Options["HeaderText"];
                _headerText = headerText.Value.ToString();
                invalidatePreview = true;
            }

            if (optionId == "FooterText")
            {
                PrintCustomTextOptionDetails footerText = (PrintCustomTextOptionDetails)sender.Options["FooterText"];
                _footerText = footerText.Value.ToString();
                invalidatePreview = true;
            }

            if (optionId == "LeftMargin")
            {
                PrintCustomTextOptionDetails leftMargin = (PrintCustomTextOptionDetails)sender.Options["LeftMargin"];
                var leftMarginValueConverterArg = double.TryParse(leftMargin.Value.ToString(), out var leftMarginValue);
                if (leftMarginValue > 50 || leftMarginValue < 0 || !leftMarginValueConverterArg)
                {
                    leftMargin.ErrorText = "Value out of range";
                    return;
                }
                else if (Math.Round(leftMarginValue, 1) != leftMarginValue)
                {
                    leftMargin.ErrorText = "Can only accept upto one decimal place";
                    return;
                }
                leftMargin.ErrorText = string.Empty;
                _applicationContentMarginLeft = (Math.Round(leftMarginValue / 100, 3));
                invalidatePreview = true;
            }

            if (optionId == "TopMargin")
            {
                PrintCustomTextOptionDetails topMargin = (PrintCustomTextOptionDetails)sender.Options["TopMargin"];
                var topMarginValueConverterArg = double.TryParse(topMargin.Value.ToString(), out var topMarginValue);
                if (Math.Round(topMarginValue, 1) != topMarginValue)
                {
                    topMargin.ErrorText = "Can only accept upto one decimal place";
                    return;
                }
                else if (topMarginValue > 100 || topMarginValue < 0 || !topMarginValueConverterArg)
                {
                    topMargin.ErrorText = "Value out of range";
                    return;
                }
                topMargin.ErrorText = string.Empty;
                _applicationContentMarginTop = (Math.Round(topMarginValue / 100, 3));
                invalidatePreview = true;
            }

            if (invalidatePreview)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _printDocument.InvalidatePreview();
                });
            }
        }

        /// <summary>
        /// This is the event handler for PrintDocument.Paginate. It creates print preview pages for the app.
        /// </summary>
        /// <param name="sender">PrintDocument</param>
        /// <param name="e">Paginate Event Arguments</param>
        private static void CreatePrintPreviewPages(object sender, PaginateEventArgs e)
        {
            lock (_printPreviewPages)
            {
                // Clear the cache of preview pages
                _printPreviewPages.Clear();

                // Clear the print canvas of preview pages
                PrintCanvas.Children.Clear();

                // This variable keeps track of the last RichTextBlockOverflow element that was added to a page which will be printed
                RichTextBlockOverflow lastRTBOOnPage;

                // Get the PrintTaskOptions
                PrintTaskOptions printingOptions = ((PrintTaskOptions)e.PrintTaskOptions);

                // Get the page description to deterimine how big the page is
                PrintPageDescription pageDescription = printingOptions.GetPageDescription(0);

                // We know there is at least one page to be printed. passing null as the first parameter to
                // AddOnePrintPreviewPage tells the function to add the first page.
                lastRTBOOnPage = AddOnePrintPreviewPage(null, pageDescription);

                // We know there are more pages to be added as long as the last RichTextBoxOverflow added to a print preview
                // page has extra content
                while (lastRTBOOnPage.HasOverflowContent && lastRTBOOnPage.Visibility == Windows.UI.Xaml.Visibility.Visible)
                {
                    lastRTBOOnPage = AddOnePrintPreviewPage(lastRTBOOnPage, pageDescription);
                }

                if (PreviewPagesCreated != null)
                {
                    PreviewPagesCreated.Invoke(_printPreviewPages, null);
                }

                PrintDocument printDoc = (PrintDocument)sender;

                // Report the number of preview pages created
                printDoc.SetPreviewPageCount(_printPreviewPages.Count, PreviewPageCountType.Intermediate);
            }
        }

        /// <summary>
        /// This is the event handler for PrintDocument.GetPrintPreviewPage. It provides a specific print preview page,
        /// in the form of an UIElement, to an instance of PrintDocument. PrintDocument subsequently converts the UIElement
        /// into a page that the Windows print system can deal with.
        /// </summary>
        /// <param name="sender">PrintDocument</param>
        /// <param name="e">Arguments containing the preview requested page</param>
        private static void GetPrintPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            PrintDocument printDoc = (PrintDocument)sender;
            printDoc.SetPreviewPage(e.PageNumber, _printPreviewPages[e.PageNumber - 1]);
        }

        /// <summary>
        /// This is the event handler for PrintDocument.AddPages. It provides all pages to be printed, in the form of
        /// UIElements, to an instance of PrintDocument. PrintDocument subsequently converts the UIElements
        /// into a pages that the Windows print system can deal with.
        /// </summary>
        /// <param name="sender">PrintDocument</param>
        /// <param name="e">Add page event arguments containing a print task options reference</param>
        private static void AddPrintPages(object sender, AddPagesEventArgs e)
        {
            // Loop over all of the preview pages and add each one to  add each page to be printied
            for (int i = 0; i < _printPreviewPages.Count; i++)
            {
                // We should have all pages ready at this point...
                _printDocument.AddPage(_printPreviewPages[i]);
            }

            PrintDocument printDoc = (PrintDocument)sender;

            // Indicate that all of the print pages have been provided
            printDoc.AddPagesComplete();
        }

        /// <summary>
        /// This function creates and adds one print preview page to the internal cache of print preview
        /// pages stored in _printPreviewPages.
        /// </summary>
        /// <param name="lastRTBOAdded">Last RichTextBlockOverflow element added in the current content</param>
        /// <param name="printPageDescription">Printer's page description</param>
        private static RichTextBlockOverflow AddOnePrintPreviewPage(RichTextBlockOverflow lastRTBOAdded, PrintPageDescription printPageDescription)
        {
            // XAML element that is used to represent to "printing page"
            FrameworkElement page;

            // The link container for text overflowing in this page
            RichTextBlockOverflow textLink;

            // Check if this is the first page ( no previous RichTextBlockOverflow)
            if (lastRTBOAdded == null)
            {
                // If this is the first page add the specific scenario content
                page = _firstPage;

                // Hide headr and footer if not provided
                StackPanel header = (StackPanel)page.FindName("Header");
                if (!string.IsNullOrEmpty(_headerText))
                {
                    header.Visibility = Visibility.Visible;
                    TextBlock headerTextBlock = (TextBlock)page.FindName("HeaderTextBlock");
                    headerTextBlock.Text = _headerText;
                }
                else
                {
                    header.Visibility = Visibility.Collapsed;
                }


                StackPanel footer = (StackPanel)page.FindName("Footer");
                if (!string.IsNullOrEmpty(_footerText))
                {
                    footer.Visibility = Visibility.Visible;
                    TextBlock footerTextBlock = (TextBlock)page.FindName("FooterTextBlock");
                    footerTextBlock.Text = _footerText;
                }
                else
                {
                    footer.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Flow content (text) from previous pages
                page = new ContinuationPage(lastRTBOAdded,
                    new FontFamily(EditorSettingsService.EditorFontFamily),
                    EditorSettingsService.EditorFontSize,
                    _headerText,
                    _footerText);
            }

            // Set "paper" width
            page.Width = printPageDescription.PageSize.Width;
            page.Height = printPageDescription.PageSize.Height;

            Grid printableArea = (Grid)page.FindName("PrintableArea");

            // Get the margins size
            // If the ImageableRect is smaller than the app provided margins use the ImageableRect
            double marginWidth = Math.Max(printPageDescription.PageSize.Width - printPageDescription.ImageableRect.Width, printPageDescription.PageSize.Width * _applicationContentMarginLeft * 2);
            double marginHeight = Math.Max(printPageDescription.PageSize.Height - printPageDescription.ImageableRect.Height, printPageDescription.PageSize.Height * _applicationContentMarginTop * 2);

            // Set-up "printable area" on the "paper"
            printableArea.Width = _firstPage.Width - marginWidth;
            printableArea.Height = _firstPage.Height - marginHeight;

            // Add the (newley created) page to the print canvas which is part of the visual tree and force it to go
            // through layout so that the linked containers correctly distribute the content inside them.
            PrintCanvas.Children.Add(page);
            PrintCanvas.InvalidateMeasure();
            PrintCanvas.UpdateLayout();

            // Find the last text container and see if the content is overflowing
            textLink = (RichTextBlockOverflow)page.FindName("ContinuationPageLinkedContainer");

            // Check if this is the last page
            if (!textLink.HasOverflowContent && textLink.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                PrintCanvas.UpdateLayout();
            }

            // Add the page to the page preview collection
            _printPreviewPages.Add(page);

            return textLink;
        }
    }
}