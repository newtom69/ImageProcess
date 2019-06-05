﻿using ImageProcessLib;
using ManageCompressedFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// back of the solution
/// manage image processing and call libraries that decode/encode images and extract images from archives/pdf
/// </summary>
public class ImageProcessBack
{
    #region members
    /// <summary>
    /// true if we want to generate un pdf file containing all images 
    /// </summary>
    public bool PdfFusion;
    /// <summary>
    /// true if we want to delete orginales files
    /// </summary>
    public bool DeleteOrigin;
    /// <summary>
    /// true if we want to delete strip around the image
    /// </summary>
    public bool DeleteStrip;
    /// <summary>
    /// Tolerance level for strips removal
    /// </summary>
    public int StripLevel;
    /// <summary>
    /// relative directory path where save image(s)
    /// </summary>
    public string PathSave;
    /// <summary>
    /// directory full path where save image(s)
    /// </summary>
    public string FullPathSave;
    /// <summary>
    /// directory full path where are files to convert
    /// </summary>
    public string FullPathOriginFiles;
    /// <summary>
    /// full name of all images we want to process
    /// </summary>
    public List<string> FullNameOfFilesToProcess;
    /// <summary>
    /// The pdf if we convert image into this format
    /// </summary>
    public PdfFile PdfToSave;
    /// <summary>
    /// type of image or document we want to save (jpg, png, pdf)
    /// </summary>
    public FileType ImageTypeToSave;
    #endregion
    public ImageProcessBack()
    {
        PdfFusion = false;
        DeleteOrigin = false;
        DeleteStrip = false;
        FullNameOfFilesToProcess = new List<string>();
        ImageTypeToSave = FileType.Unknow;
    }
    /// <summary>
    /// process all images or files containing images in <c>FullNameOfImagesToProcess</c>
    /// </summary>
    /// <returns>string with all warning and errors for show in a MessageBox or similar to alert user</returns>
    public string Process()
    {
        string listErrors = "";
        if (FullNameOfFilesToProcess.Count == 0)
        {
            listErrors = "Erreur\nMerci de choisir des images";
            return listErrors;
        }
        if (PdfFusion)
        {
            PdfToSave = new PdfFile();
        }
        foreach (string fullNameOfFile in FullNameOfFilesToProcess)
        {
            List<string> imagesFullNames = new List<string>();
            FileType fileToReadType = FileType.Unknow;
            string mimeType = MimeType.getFromFile(fullNameOfFile);
            switch (mimeType)
            {
                case "application/pdf":
                    fileToReadType = FileType.Pdf;
                    imagesFullNames = PdfFile.ExtractImagesToTempPath(fullNameOfFile);
                    PathSave = Path.GetFileNameWithoutExtension(fullNameOfFile);
                    break;
                case "application/octet-stream":
                case "application/x-rar-compressed":
                case "application/x-zip-compressed":
                case "multipart/x-zip":
                    fileToReadType = FileType.Archive;
                    imagesFullNames = CompressedFile.ExtractFilesToTempPath(fullNameOfFile);
                    PathSave = Path.GetFileNameWithoutExtension(fullNameOfFile);
                    break;
                case var someVal when new Regex(@"image/.*").IsMatch(someVal):
                    fileToReadType = FileType.Image;
                    imagesFullNames.Add(fullNameOfFile);
                    break;
                default:
                    break;
            }
            if (imagesFullNames.Count == 0)
            {
                listErrors += "pas d'images trouvées dans " + fullNameOfFile + "\n";
            }
            FullPathSave = Path.Combine(FullPathOriginFiles, PathSave);
            foreach (string imageFullName in imagesFullNames)
            {
                try
                {
                    if (!DeleteStrip && PdfFusion)
                    {
                        PdfToSave.AddImage(imageFullName);
                    }
                    else
                    {
                        using (ImageProcess imageToProcess = new ImageProcess(imageFullName))
                        {
                            if (DeleteStrip)
                            {
                                imageToProcess.DeleteStrips(StripLevel);
                            }
                            if (PdfFusion)
                            {
                                MemoryStream memoryStream = new MemoryStream();
                                imageToProcess.Save(memoryStream);
                                PdfToSave.AddImage(ref memoryStream);
                            }
                            else
                            {
                                imageToProcess.SaveTo(ImageTypeToSave, FullPathSave);
                            }
                        }
                    }
                    if (DeleteOrigin || fileToReadType == FileType.Archive || fileToReadType == FileType.Pdf)
                    {
                        File.Delete(imageFullName);
                    }
                }
                catch (Exception ex)
                {
                    listErrors += "Avertissement : " + ex.Message + " sur fichier " + imageFullName + "\n";
                }
            }
            if (DeleteOrigin && (fileToReadType == FileType.Archive || fileToReadType == FileType.Pdf))
            {
                File.Delete(fullNameOfFile);
            }
        }
        if (PdfFusion)
        {
            try
            {
                PdfToSave.Save(FullPathSave, "mergedImages.pdf");
            }
            catch (Exception ex)
            {
                listErrors += "Erreur : " + ex.Message;
            }
        }
        string contentEnd = "Fin de traitement\n";
        return contentEnd + listErrors;
    }
}

