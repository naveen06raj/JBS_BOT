using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ERP.API.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;
using Npgsql;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;

namespace ERP.API.Controllers
{
    /// <summary>
    /// Controller for managing demo grid operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class DemoGridController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<DemoGridController> _logger;

        public DemoGridController(IConfiguration configuration, ILogger<DemoGridController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection string is not configured");
            _logger = logger;
        }

        /// <summary>
        /// Get a paginated list of demos based on filters and sorting criteria
        /// </summary>
        /// <param name="request">The request containing search, filter, pagination and sorting parameters</param>
        /// <returns>A list of demos matching the criteria with total count</returns>
        /// <response code="200">Returns the list of demos with total count</response>
        /// <response code="400">If the request parameters are invalid</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost("grid")]
        [ProducesResponseType(typeof(DemoGridResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DemoGridResult>> GetDemosGrid([FromBody] DemoGridRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid request parameters",
                        statusCode = 400,
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                _logger.LogInformation("Database connection opened successfully");

                // Clean up the request arrays to remove "string" values
                request.CustomerNames = request.CustomerNames?.Where(x => x != "string").ToArray() ?? Array.Empty<string>();
                request.Statuses = request.Statuses?.Where(x => x != "string").ToArray() ?? Array.Empty<string>();
                request.DemoApproaches = request.DemoApproaches?.Where(x => x != "string").ToArray() ?? Array.Empty<string>();
                request.DemoOutcomes = request.DemoOutcomes?.Where(x => x != "string").ToArray() ?? Array.Empty<string>();
                request.SearchText = request.SearchText == "string" ? null : request.SearchText;

                _logger.LogInformation("Cleaned request parameters: {@Request}", request);

                var sql = "SELECT * FROM fn_get_sales_demos_grid(@request::jsonb)";
                using var command = new NpgsqlCommand(sql, connection);

                // Create JSON request parameter
                var jsonRequest = new
                {
                    searchText = request.SearchText,
                    customerNames = request.CustomerNames,
                    statuses = request.Statuses,
                    demoApproaches = request.DemoApproaches,
                    demoOutcomes = request.DemoOutcomes,
                    startDate = request.StartDate,
                    endDate = request.EndDate,
                    pageNumber = request.PageNumber,
                    pageSize = request.PageSize,
                    orderBy = request.OrderBy?.ToLower() ?? "date_created",
                    orderDirection = request.OrderDirection?.ToUpper() ?? "DESC"
                };

                var jsonStr = System.Text.Json.JsonSerializer.Serialize(jsonRequest);
                _logger.LogInformation("Executing query with JSON parameters: {JsonStr}", jsonStr);
                command.Parameters.AddWithValue("request", jsonStr);

                var results = new List<DemoGridResult>();
                int totalRecords = 0;

                try 
                {
                    using var reader = await command.ExecuteReaderAsync();
                    
                    if (!reader.HasRows)
                    {
                        _logger.LogInformation("No rows returned from query");
                    }

                    while (await reader.ReadAsync())
                    {
                        totalRecords = reader.GetInt32(0);
                        var result = new DemoGridResult
                        {
                            Id = reader.GetInt32(1),
                            UserCreated = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                            DateCreated = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                            UserUpdated = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                            DateUpdated = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                            UserId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                            DemoDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                            Status = reader.IsDBNull(8) ? null : reader.GetString(8),
                            CustomerName = reader.IsDBNull(9) ? null : reader.GetString(9),
                            DemoName = reader.IsDBNull(10) ? null : reader.GetString(10),
                            DemoContact = reader.IsDBNull(11) ? null : reader.GetString(11),
                            DemoApproach = reader.IsDBNull(12) ? null : reader.GetString(12),
                            DemoOutcome = reader.IsDBNull(13) ? null : reader.GetString(13),
                            DemoFeedback = reader.IsDBNull(14) ? null : reader.GetString(14),
                            Comments = reader.IsDBNull(15) ? null : reader.GetString(15),
                            OpportunityId = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                            PresenterId = reader.IsDBNull(17) ? null : reader.GetInt32(17),
                            PresenterName = reader.IsDBNull(18) ? null : reader.GetString(18),
                            AddressId = reader.IsDBNull(19) ? null : reader.GetInt32(19),
                            CustomerId = reader.IsDBNull(20) ? null : reader.GetInt32(20),
                            OpportunityName = reader.IsDBNull(21) ? null : reader.GetString(21),
                            AddressDetails = reader.IsDBNull(22) ? null : reader.GetString(22),
                            UserCreatedName = reader.IsDBNull(23) ? null : reader.GetString(23),
                            UserUpdatedName = reader.IsDBNull(24) ? null : reader.GetString(24)
                        };
                        results.Add(result);
                        _logger.LogInformation("Added result with ID: {Id}, CustomerName: {CustomerName}", result.Id, result.CustomerName);
                    }

                    _logger.LogInformation("Query completed. Found {Count} results with {TotalRecords} total records", 
                        results.Count, totalRecords);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing demo grid query: {Message}", ex.Message);
                    throw;
                }

                return Ok(new { 
                    data = results,
                    totalRecords = totalRecords,
                    pageNumber = request.PageNumber,
                    pageSize = request.PageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDemosGrid: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving demo grid data",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Get dropdown options for filtering demos
        /// </summary>
        /// <returns>Available options for statuses, customer names and demo types</returns>
        /// <response code="200">Returns the dropdown options</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet("dropdown-options")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetDropdownOptions()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();                var sql = @"
                    SELECT ARRAY_AGG(DISTINCT status) FILTER (WHERE status IS NOT NULL) as statuses,
                           ARRAY_AGG(DISTINCT customer_name) FILTER (WHERE customer_name IS NOT NULL) as customer_names,
                           ARRAY_AGG(DISTINCT demo_approach) FILTER (WHERE demo_approach IS NOT NULL) as demo_approaches,
                           ARRAY_AGG(DISTINCT demo_outcome) FILTER (WHERE demo_outcome IS NOT NULL) as demo_outcomes
                    FROM sales_demos;";

                using var command = new NpgsqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {                    return Ok(new                    {
                        Statuses = reader.IsDBNull(0) ? Array.Empty<string>() : (string[])reader.GetValue(0),
                        CustomerNames = reader.IsDBNull(1) ? Array.Empty<string>() : (string[])reader.GetValue(1),
                        DemoApproaches = reader.IsDBNull(2) ? Array.Empty<string>() : (string[])reader.GetValue(2),
                        DemoOutcomes = reader.IsDBNull(3) ? Array.Empty<string>() : (string[])reader.GetValue(3)
                    });
                }                return Ok(new
                {
                    Statuses = Array.Empty<string>(),
                    CustomerNames = Array.Empty<string>(),
                    DemoApproaches = Array.Empty<string>(),
                    DemoOutcomes = Array.Empty<string>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting dropdown options: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to retrieve dropdown options",
                    statusCode = 500,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Export demos to Excel
        /// </summary>
        /// <param name="request">The request containing filter parameters</param>
        /// <returns>Excel file containing filtered demos</returns>
        /// <response code="200">Returns the Excel file</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost("export/excel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportToExcel([FromBody] DemoGridRequest request)
        {
            try
            {
                // Get all results for export (no pagination)
                request.PageSize = int.MaxValue;
                request.PageNumber = 1;

                var results = await GetDemosGrid(request);
                if (results.Result is not OkObjectResult okResult)
                {
                    return results.Result;
                }

                dynamic responseData = okResult.Value;
                var demos = (IEnumerable<DemoGridResult>)responseData.Results;

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Demos");

                // Define headers
                var headers = new[] { "Demo ID", "Customer Name", "Demo Name", "Demo Type", "Status", "Demo Date/Time", 
                    "Demo Contact", "Demo Approach", "Demo Outcome", "Demo Feedback", "Comments", "Presenter Name" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                // Populate data
                int row = 2;
                foreach (var demo in demos)
                {
                    worksheet.Cell(row, 1).Value = demo.Id;
                    worksheet.Cell(row, 2).Value = demo.CustomerName ?? "";
                    worksheet.Cell(row, 3).Value = demo.DemoName ?? "";
                    worksheet.Cell(row, 4).Value = ""; // DemoType removed
                    worksheet.Cell(row, 5).Value = demo.Status ?? "";
                    worksheet.Cell(row, 6).Value = demo.DemoDate?.ToString("yyyy-MM-dd HH:mm") ?? "";
                    worksheet.Cell(row, 7).Value = demo.DemoContact ?? "";
                    worksheet.Cell(row, 8).Value = demo.DemoApproach ?? "";
                    worksheet.Cell(row, 9).Value = demo.DemoOutcome ?? "";
                    worksheet.Cell(row, 10).Value = demo.DemoFeedback ?? "";
                    worksheet.Cell(row, 11).Value = demo.Comments ?? "";
                    worksheet.Cell(row, 12).Value = demo.PresenterName ?? "";
                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Convert to byte array
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                string fileName = $"Demos_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while exporting to Excel: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to export demos to Excel",
                    statusCode = 500,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Export demos to PDF
        /// </summary>
        /// <param name="request">The request containing filter parameters</param>
        /// <returns>PDF file containing filtered demos</returns>
        /// <response code="200">Returns the PDF file</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost("export/pdf")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExportToPdf([FromBody] DemoGridRequest request)
        {
            MemoryStream? stream = null;
            PdfWriter? writer = null;
            PdfDocument? pdf = null;
            Document? document = null;

            try
            {
                // Get all results for export (no pagination)
                request.PageSize = int.MaxValue;
                request.PageNumber = 1;

                var results = await GetDemosGrid(request);
                if (results.Result is not OkObjectResult okResult)
                {
                    return results.Result;
                }

                dynamic responseData = okResult.Value;
                var demos = (IEnumerable<DemoGridResult>)responseData.Results;

                stream = new MemoryStream();
                writer = new PdfWriter(stream);
                pdf = new PdfDocument(writer);
                document = new Document(pdf);

                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                document.Add(new Paragraph("Demos Report")
                    .SetFont(boldFont)
                    .SetFontSize(16)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20));

                document.Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                    .SetFont(normalFont)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetMarginBottom(20));

                var table = new Table(new float[] { 1, 2, 1.5f, 1, 1, 1.5f, 1.5f, 1.5f })
                    .UseAllAvailableWidth()
                    .SetFixedLayout();

                var headers = new[] { "Demo ID", "Customer Name", "Demo Name", "Demo Type", "Status", "Demo Date", "Contact", "Presenter" };
                foreach (var header in headers)
                {
                    table.AddHeaderCell(
                        new Cell()
                            .Add(new Paragraph(header).SetFont(boldFont))
                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetPadding(5)
                    );
                }

                foreach (var demo in demos)
                {
                    table.AddCell(new Cell().Add(new Paragraph(demo.Id.ToString()).SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(demo.CustomerName ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(demo.DemoName ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph("").SetFont(normalFont)).SetPadding(5)); // DemoType removed
                    table.AddCell(new Cell().Add(new Paragraph(demo.Status ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(demo.DemoDate?.ToString("yyyy-MM-dd") ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(demo.DemoContact ?? "").SetFont(normalFont)).SetPadding(5));
                    table.AddCell(new Cell().Add(new Paragraph(demo.PresenterName ?? "").SetFont(normalFont)).SetPadding(5));
                }

                document.Add(table);

                document.Close();
                pdf.Close();
                writer.Close();

                var bytes = stream.ToArray();
                string fileName = $"Demos_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while exporting to PDF: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    message = "Failed to export demos to PDF",
                    statusCode = 500,
                    error = ex.Message
                });
            }
            finally
            {
                try
                {
                    document?.Close();
                    pdf?.Close();
                    writer?.Close();
                    stream?.Dispose();
                }
                catch
                {
                    // Suppress any errors during cleanup
                }
            }
        }
    }
}
