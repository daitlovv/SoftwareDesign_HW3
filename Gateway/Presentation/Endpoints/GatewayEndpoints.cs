using Gateway.Application.DTOs;
using Gateway.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Presentation.Endpoints;

public static class GatewayEndpoints
{
    public static void MapGatewayEndpoints(this WebApplication app)
    {
        app.MapPost("/works/upload", async (HttpContext context, [FromServices] GatewayService service) =>
        {
            string studentName = context.Request.Query["studentName"].ToString();
            string workId = context.Request.Query["workId"].ToString();
            
            if (!context.Request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Ожидается multipart/form-data" });
            }
            
            IFormCollection form = await context.Request.ReadFormAsync();
            IFormFile file = form.Files["file"];
            
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "Файл обязателен для загрузки" });
            }

            if (string.IsNullOrWhiteSpace(studentName))
            {
                return Results.BadRequest(new { error = "Имя студента обязательно" });
            }

            if (string.IsNullOrWhiteSpace(workId))
            {
                return Results.BadRequest(new { error = "Идентификатор работы обязателен" });
            }

            try
            {
                ReportDto report = await service.UploadWorkAsync(studentName, workId, file);
                return Results.Created($"/works/{workId}/reports", report);
            }
            catch (ArgumentException argEx)
            {
                return Results.BadRequest(new { error = "Неверные параметры запроса", details = argEx.Message });
            }
            catch (HttpRequestException httpEx)
            {
                return Results.Json(new { error = "Сервис недоступен", details = httpEx.Message }, 
                    statusCode: 503);
            }
            catch (TimeoutException timeEx)
            {
                return Results.Json(new { error = "Время ожидания истекло", details = timeEx.Message }, 
                    statusCode: 504);
            }
            catch (UnauthorizedAccessException authEx)
            {
                return Results.Json(new { error = "Нет доступа к сервису", details = authEx.Message }, 
                    statusCode: 403);
            }
            catch (InvalidOperationException invOpEx)
            {
                return Results.Json(new { error = "Неверная операция", details = invOpEx.Message }, 
                    statusCode: 400);
            }
            catch (FileNotFoundException fileEx)
            {
                return Results.NotFound(new { error = "Файл не найден", details = fileEx.Message });
            }
            catch (KeyNotFoundException keyEx)
            {
                return Results.NotFound(new { error = "Ресурс не найден", details = keyEx.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Внутренняя ошибка сервера: {ex.Message}", statusCode: 500);
            }
        })
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<ReportDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status503ServiceUnavailable)
        .WithTags("Работы")
        .WithName("UploadWork");

        app.MapGet("/works/{workId}/reports", async (string workId, [FromServices] GatewayService service) =>
        {
            if (string.IsNullOrWhiteSpace(workId))
            {
                return Results.BadRequest(new { error = "Идентификатор работы обязателен" });
            }

            try
            {
                System.Collections.Generic.IEnumerable<ReportDto> reports = await service.GetReportsAsync(workId);
                return Results.Ok(reports);
            }
            catch (ArgumentException argEx)
            {
                return Results.BadRequest(new { error = "Неверный идентификатор работы", details = argEx.Message });
            }
            catch (HttpRequestException httpEx)
            {
                return Results.Json(new { error = "Сервис анализа недоступен", details = httpEx.Message }, 
                    statusCode: 503);
            }
            catch (KeyNotFoundException keyEx)
            {
                return Results.NotFound(new { error = "Отчеты не найдены", details = keyEx.Message });
            }
            catch (InvalidOperationException invOpEx)
            {
                return Results.Json(new { error = "Неверная операция", details = invOpEx.Message }, 
                    statusCode: 400);
            }
            catch (TimeoutException timeEx)
            {
                return Results.Json(new { error = "Время ожидания истекло", details = timeEx.Message }, 
                    statusCode: 504);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Внутренняя ошибка: {ex.Message}", statusCode: 500);
            }
        })
        .Produces<System.Collections.Generic.IEnumerable<ReportDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status503ServiceUnavailable)
        .WithTags("Работы")
        .WithName("GetWorkReports");
    }
}