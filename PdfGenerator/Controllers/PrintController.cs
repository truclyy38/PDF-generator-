using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PdfGenerator.Extensions;
using PdfGenerator.Services.Meta;
using PdfGenerator.ViewModels;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using ZXing.Common;
using ZXing;
using ZXing.Rendering;
using System.Collections;
using System.IO;
using System.Drawing.Imaging;
using Microsoft.AspNetCore.Hosting;

namespace PdfGenerator.Controllers
{
    [Route("/api/print")]
    public class PrintController : ControllerBase
    {
        private readonly ITemplateService _templateService;
        private readonly IHostingEnvironment _env;


        public PrintController(ITemplateService templateService, IHostingEnvironment env)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Print()
        {
            //BarcodeWriterPixelData writer = new BarcodeWriterPixelData()
            //{
            //    Format = BarcodeFormat.PDF_417,
            //    Options = new EncodingOptions
            //    {
            //        Height = 400,
            //        Width = 800,
            //        PureBarcode = false, // this should indicate that the text should be displayed, in theory. Makes no difference, though.
            //        Margin = 10
            //    }
            //};
            //var barcode = new Barcode("543534"); // default: Code128

            //var image = barcode.GetImage();


            //var base64 = image.ToBase64String(PngFormat.Instance);
            //var value = barcode.GetBase64Image();
            byte[] byteArray;

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.PDF_417,
                Options = new EncodingOptions { Width = 400, Height = 200 } //optional
            };
            var imgBitmap = writer.Write("Hello123");
            using (var stream = new MemoryStream())
            {
                imgBitmap.Save(stream, ImageFormat.Png);
                byteArray = stream.ToArray();
            }

            var logo = System.IO.File.ReadAllBytes(Path.Combine(_env.ContentRootPath, "Assets/logo.png"));
            var logoUrl = Path.Combine(_env.ContentRootPath, "Assets\\logo.png");

            var model = new InvoiceViewModel
            {
                CreatedAt = DateTime.Now,
                Due = DateTime.Now.AddDays(10),
                Id = 12533,
                AddressLine = "Jumpy St. 99",
                City = "Trampoline",
                ZipCode = "22-113",
                CompanyName = "Jumping Rabbit Co.",
                PaymentMethod = "Check",
                Items = new List<InvoiceItemViewModel>
                {
                    new InvoiceItemViewModel("Website design", 621.99m),
                    new InvoiceItemViewModel("Website creation", 1231.99m)
                },
                Image = Convert.ToBase64String(byteArray),
                UrlContent = Convert.ToBase64String(logo)           ,
                Url = "C:/Users/Trucly/Downloads/pdfgenerator-main/pdfgenerator/iIages/logo.png"
            };
            var html = await _templateService.RenderAsync("Templates/InvoiceTemplate", model);
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = PuppeteerExtensions.ExecutablePath
            });
            await using var page = await browser.NewPageAsync();
            await page.EmulateMediaTypeAsync(MediaType.Screen);
            await page.SetContentAsync(html);
            var pdfContent = await page.PdfStreamAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true
            });

            //return new ContentResult { ContentType = "text/html", Content = pdfContent };
            return File(pdfContent, "application/pdf", $"Invoice-{model.Id}.pdf");
        }

       
    }

 
}