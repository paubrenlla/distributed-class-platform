using AuthGrpc;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Common;
using Common.DTOs;

namespace AuthGrpc.Services
{
    public class AuthService : Auth.AuthBase
    {
        private readonly string _host;
        private readonly int _port;

        public AuthService(IConfiguration cfg)
        {
            _host = cfg["AUTH_SERVER_HOST"] ?? "localhost";
            _port = int.TryParse(cfg["AUTH_SERVER_PORT"], out var p) ? p : 5000;
        }

        public override async Task<ValidateUserResponse> ValidateUser(ValidateUserRequest request, ServerCallContext context)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(_host, _port);

                var helper = new Common.NetworkDataHelper(socket);

                var dto = new Common.DTOs.LoginRequestDTO
                {
                    Username = request.Username,
                    Password = request.Password
                };
                var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dto));

                await helper.Send(new Common.Frame
                {
                    Header  = Common.ProtocolConstants.Request,
                    Command = Common.ProtocolConstants.CommandValidateUser,
                    Data    = payload
                });

                var resp = await helper.Receive();
                var text = Encoding.UTF8.GetString(resp.Data);
                var ok = text.StartsWith("OK|", StringComparison.OrdinalIgnoreCase);

                return new ValidateUserResponse { Ok = ok, Reason = ok ? "" : text };
            }
            catch (Exception ex)
            {
                return new ValidateUserResponse { Ok = false, Reason = ex.Message };
            }
        }

        public override async Task<ValidateClassLinkResponse> ValidateClassLink(ValidateClassLinkRequest request, ServerCallContext context)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(_host, _port);

                var helper = new Common.NetworkDataHelper(socket);
                var payload = Encoding.UTF8.GetBytes(request.Link);

                await helper.Send(new Common.Frame
                {
                    Header  = Common.ProtocolConstants.Request,
                    Command = Common.ProtocolConstants.CommandValidateClassLink,
                    Data    = payload
                });

                var resp = await helper.Receive();
                var text = Encoding.UTF8.GetString(resp.Data);
                var ok = text.StartsWith("OK|", StringComparison.OrdinalIgnoreCase);

                return new ValidateClassLinkResponse { Ok = ok, Reason = ok ? "" : text };
            }
            catch (Exception ex)
            {
                return new ValidateClassLinkResponse { Ok = false, Reason = ex.Message };
            }
        }

		public override async Task<ValidateEnrollmentResponse> ValidateEnrollment(
    		ValidateEnrollmentRequest request, ServerCallContext context)
		{
   		 	try
    		{
        		using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        		await socket.ConnectAsync(_host, _port);

        		var helper = new NetworkDataHelper(socket);

        		var payloadObj = new { Username = request.Username, Link = request.Link };
        		var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payloadObj));

        		await helper.Send(new Frame
        		{
            		Header = ProtocolConstants.Request,
            		Command = ProtocolConstants.CommandValidateEnrollment,
            		Data = payload
        		});

        		var resp = await helper.Receive();
        		var text = Encoding.UTF8.GetString(resp.Data);
        		var ok = text.StartsWith("OK|", StringComparison.OrdinalIgnoreCase);

        		return new ValidateEnrollmentResponse { Ok = ok, Reason = ok ? "" : text };
    		}
    		catch (Exception ex)
    		{
        		return new ValidateEnrollmentResponse { Ok = false, Reason = ex.Message };
    		}
		}


    }
}
