using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using Repository;
using Newtonsoft.Json;

namespace Server.Services
{
    public class WebhookNotificationService
    {
        private readonly InscriptionRepository _inscriptionRepo;
        private readonly HttpClient _httpClient;

        public WebhookNotificationService(InscriptionRepository inscriptionRepo)
        {
            _inscriptionRepo = inscriptionRepo;
            _httpClient = new HttpClient();
        }

        public async Task StartAsync(CancellationToken token)
        {
            Console.WriteLine("--> Servicio de Webhooks iniciado.");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    CheckAndNotify();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en el servicio de Webhooks: {ex.Message}");
                }

                await Task.Delay(10000, token); 
            }
        }

        private void CheckAndNotify()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            
            var inscriptionsToNotify = _inscriptionRepo.GetAll() 
                .Where(i => !string.IsNullOrEmpty(i.WebhookUrl) && 
                            !i.NotificationSent &&
                            i.Status == InscriptionStatus.Active &&
                            i.Class.StartDate > now && 
                            (i.Class.StartDate - now).TotalMinutes <= 1)
                .ToList();

            foreach (var inscription in inscriptionsToNotify)
            {
                _ = SendNotificationAsync(inscription);
                inscription.NotificationSent = true; 
            }
        }

        private async Task SendNotificationAsync(Inscription inscription)
        {
            try
            {
                var payload = new
                {
                    Message = $"¡Tu clase '{inscription.Class.Name}' está por comenzar!",
                    ClassId = inscription.Class.Id,
                    StartTime = inscription.Class.StartDate
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"[Webhook] Enviando notificación a {inscription.User.Username} ({inscription.WebhookUrl})...");
                
                var response = await _httpClient.PostAsync(inscription.WebhookUrl, content);
                
                if (response.IsSuccessStatusCode)
                    Console.WriteLine($"[Webhook] Enviado correctamente a {inscription.User.Username}.");
                else
                    Console.WriteLine($"[Webhook] Falló el envío a {inscription.User.Username}. Status: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Webhook] Error enviando a {inscription.User.Username}: {ex.Message}");
            }
        }
    }
}