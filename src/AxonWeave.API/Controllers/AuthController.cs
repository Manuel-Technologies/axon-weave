using AxonWeave.API.Abstractions;
using AxonWeave.API.Services;
using AxonWeave.Application.Common.Interfaces;
using AxonWeave.Application.DTOs.Auth;
using AxonWeave.Application.DTOs.Common;
using AxonWeave.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AxonWeave.Application.Options;

namespace AxonWeave.API.Controllers;

[Route("api/auth")]
public class AuthController : AuthenticatedControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly OtpOptions _otpOptions;
    private readonly JwtOptions _jwtOptions;

    public AuthController(IUnitOfWork unitOfWork, IOtpService otpService, IJwtTokenGenerator jwtTokenGenerator, IOptions<OtpOptions> otpOptions, IOptions<JwtOptions> jwtOptions)
    {
        _unitOfWork = unitOfWork;
        _otpService = otpService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _otpOptions = otpOptions.Value;
        _jwtOptions = jwtOptions.Value;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
        var normalizedName = request.Name.Trim();
        var user = await _unitOfWork.Users.Query().FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                PhoneNumber = normalizedPhone,
                Name = normalizedName
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            user.Name = normalizedName;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.Users.Update(user);
        }

        var code = _otpService.GenerateCode();
        var existingOtps = await _unitOfWork.PendingOtps.Query()
            .Where(x => x.PhoneNumber == normalizedPhone)
            .ToListAsync(cancellationToken);
        foreach (var otp in existingOtps)
        {
            _unitOfWork.PendingOtps.Remove(otp);
        }

        var pendingOtp = new PendingOtp
        {
            PhoneNumber = normalizedPhone,
            CodeHash = _otpService.HashCode(code),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_otpOptions.ExpiryMinutes)
        };

        await _unitOfWork.PendingOtps.AddAsync(pendingOtp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<RegisterResponse>
        {
            Data = new RegisterResponse
            {
                PhoneNumber = normalizedPhone,
                Name = user.Name,
                OtpExpiresAt = pendingOtp.ExpiresAt,
                DevelopmentOtp = _otpOptions.UseStaticOtp ? code : null
            }
        });
    }

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
        var otp = await _unitOfWork.PendingOtps.Query()
            .Where(x => x.PhoneNumber == normalizedPhone)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || otp.ExpiresAt < DateTimeOffset.UtcNow || !_otpService.Verify(request.Code.Trim(), otp.CodeHash))
        {
            return Unauthorized(new { message = "Invalid or expired OTP code." });
        }

        var user = await _unitOfWork.Users.Query().FirstAsync(x => x.PhoneNumber == normalizedPhone, cancellationToken);
        var token = _jwtTokenGenerator.GenerateToken(user);
        _unitOfWork.PendingOtps.Remove(otp);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<AuthResponse>
        {
            Data = new AuthResponse
            {
                Token = token,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
                User = user.ToDto()
            }
        });
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var normalized = phoneNumber.Trim().Replace(" ", string.Empty);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Phone number is required.");
        }

        return normalized;
    }
}
