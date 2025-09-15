using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class User
    {
        private static int _nextId = 1;
        public int Id { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool Activo { get; private set; }
        public Usuario(string username, string password, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("username es requerido", nameof(username));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password es requerido", nameof(password));

            Id = _nextId++;
            Username = username.Trim();
            Password = password;
            CreatedAt = DateTime.Today;
            Activo = true;
        }

        public bool VerificarPassword(string password)
        {
            if (password == null) return false;
            return Password == password;
        }

        public void Desactivar()
        {
            Activo = false;
        }

        public void Activar()
        {
            Activo = true;
        }

        public override bool Equals(object obj)
        {
            if (obj is Usuario other)
            {
                return string.Equals(this.Username, other.Username, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override string ToString()
        {
            return $"Usuario{{Id={Id}, Username={Username}, CreatedAt={CreatedAt}, Activo={Activo}}}";
        }
    }
}
}
