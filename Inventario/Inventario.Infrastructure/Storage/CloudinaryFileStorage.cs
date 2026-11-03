using System.Text.RegularExpressions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Inventario.Application.Interfaces;
using Inventario.Core.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Inventario.Infrastructure.Storage;

/// <summary>
/// Guarda las fotos del inventario en Cloudinary y devuelve su URL pública (https).
/// No usa disco local: funciona igual en local y en un contenedor sin volumen.
/// Al reemplazar o eliminar un bien, DeleteAsync borra el archivo real de Cloudinary.
/// </summary>
public class CloudinaryFileStorage : IFileStorage
{
    private const int MaxBytes = 3_000_000;
    private const string DefaultFolder = "camara-comercio-huancayo/inventario";

    private static readonly Regex DataUrl = new(
        @"^data:image/(?<t>jpeg|jpg|png|webp);base64,(?<d>.+)$",
        RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryFileStorage> _logger;
    private readonly string _cloudName;
    private readonly string _folder;

    public CloudinaryFileStorage(IConfiguration cfg, ILogger<CloudinaryFileStorage> logger)
    {
        _logger = logger;
        _cloudName = cfg["Cloudinary:CloudName"]?.Trim() ?? "";
        var apiKey = cfg["Cloudinary:ApiKey"]?.Trim() ?? "";
        var apiSecret = cfg["Cloudinary:ApiSecret"]?.Trim() ?? "";
        _folder = string.IsNullOrWhiteSpace(cfg["Cloudinary:Folder"])
            ? DefaultFolder
            : cfg["Cloudinary:Folder"]!.Trim().Trim('/');

        if (string.IsNullOrWhiteSpace(_cloudName) || _cloudName == "tu-cloud-name"
            || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            _logger.LogWarning("Cloudinary no está configurado. Las fotos del inventario no se podrán guardar.");
            return;
        }

        _cloudinary = new Cloudinary(new Account(_cloudName, apiKey, apiSecret));
    }

    public async Task<string> SaveImageAsync(string dataUrl)
    {
        if (_cloudinary is null)
            throw new StorageUnavailableException(
                "El almacenamiento de fotos no está configurado. Contacta al administrador.");

        var match = DataUrl.Match(dataUrl);
        if (!match.Success)
            throw new BusinessRuleException("La foto no tiene un formato válido.");

        byte[] bytes;
        try { bytes = Convert.FromBase64String(match.Groups["d"].Value); }
        catch (FormatException) { throw new BusinessRuleException("La foto está dañada."); }

        if (bytes.Length == 0) throw new BusinessRuleException("La foto está vacía.");
        if (bytes.Length > MaxBytes) throw new BusinessRuleException("La foto es demasiado grande.");

        var extension = DetectExtension(bytes)
            ?? throw new BusinessRuleException("La foto no es una imagen válida.");

        try
        {
            await using var stream = new MemoryStream(bytes);
            var upload = new ImageUploadParams
            {
                File = new FileDescription($"inventario-{Guid.NewGuid():N}.{extension}", stream),
                Folder = _folder,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation()
                    .Width(1600)
                    .Height(1600)
                    .Crop("limit")
                    .Quality(85)
            };

            var result = await _cloudinary.UploadAsync(upload);
            if (result.Error is not null)
            {
                _logger.LogError("Cloudinary rechazó la subida: {Message}", result.Error.Message);
                throw new StorageUnavailableException("No se pudo subir la foto. Intenta de nuevo.");
            }

            var url = result.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(url))
                throw new StorageUnavailableException("No se pudo obtener la URL de la foto.");

            return url;
        }
        catch (StorageUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir una foto a Cloudinary");
            throw new StorageUnavailableException("No se pudo subir la foto. Intenta de nuevo.");
        }
    }

    public async Task DeleteAsync(string? url)
    {
        if (_cloudinary is null || string.IsNullOrWhiteSpace(url)) return;

        var publicId = ExtractPublicId(url);
        if (publicId is null) return;

        // Cloudinary puede fallar por un corte de red momentáneo. Reintentamos
        // unas pocas veces antes de dejar un archivo huérfano.
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                if (result.Error is not null)
                    throw new InvalidOperationException(result.Error.Message);

                return;
            }
            catch (Exception ex)
            {
                if (attempt == 3)
                {
                    _logger.LogWarning(ex,
                        "No se pudo borrar la foto {PublicId} de Cloudinary después de 3 intentos", publicId);
                    return;
                }

                _logger.LogWarning(
                    "Reintento {Attempt}/3 al borrar la foto {PublicId}: {Message}",
                    attempt, publicId, ex.Message);
                await Task.Delay(300 * attempt);
            }
        }
    }

    /// <summary>
    /// Extrae el public_id solo de URLs de nuestra cuenta y carpeta.
    /// Así nunca se borra una imagen externa o de otra parte del proyecto.
    /// </summary>
    private string? ExtractPublicId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        if (!uri.Host.Equals("res.cloudinary.com", StringComparison.OrdinalIgnoreCase)) return null;

        var parts = uri.AbsolutePath
            .TrimStart('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 4) return null;
        if (!parts[0].Equals(_cloudName, StringComparison.OrdinalIgnoreCase)) return null;
        if (!parts[1].Equals("image", StringComparison.OrdinalIgnoreCase)) return null;
        if (!parts[2].Equals("upload", StringComparison.OrdinalIgnoreCase)) return null;

        var rest = parts.Skip(3).ToList();

        // Quita la versión: /upload/v1699999999/...
        if (rest.Count > 0 && rest[0].StartsWith('v') && rest[0].Length > 1 && rest[0][1..].All(char.IsDigit))
            rest.RemoveAt(0);

        if (rest.Count == 0) return null;

        var publicId = string.Join('/', rest);

        // Quita la extensión final (.jpg, .png, .webp).
        var dot = publicId.LastIndexOf('.');
        if (dot > publicId.LastIndexOf('/')) publicId = publicId[..dot];

        // Solo borra archivos de la carpeta del inventario.
        if (!publicId.StartsWith(_folder + "/", StringComparison.Ordinal)) return null;
        return publicId;
    }

    private static string? DetectExtension(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "jpg";

        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "png";

        if (bytes.Length >= 12
            && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
            && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return "webp";

        return null;
    }
}
