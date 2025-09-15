using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class OnlineClass
    {
        private static int _nextId = 1;

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public int MaxCapacity { get; private set; }
        public DateTimeOffset StartDate { get; private set; }
        public int Duration { get; private set; }
        public string Link { get; private set; }
        public string Image { get; private set; } // ruta o nombre de archivo
        public User Creator { get; private set; }

        private readonly List<User> _registered;
        public IReadOnlyCollection<User> Registered => _registered.AsReadOnly();

        public OnlineClass(
            string nombre,
            string descripcion,
            int cupoMaximo,
            DateTimeOffset fechaHoraInicio,
            int duracionMinutos,
            User creador,
            string imagenPortada = null
        )
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido", nameof(nombre));
            if (string.IsNullOrWhiteSpace(descripcion)) throw new ArgumentException("Descripción requerida", nameof(descripcion));
            if (cupoMaximo <= 0) throw new ArgumentException("Cupo máximo inválido", nameof(cupoMaximo));
            if (duracionMinutos <= 0) throw new ArgumentException("Duración inválida", nameof(duracionMinutos));
            if (creador == null) throw new ArgumentNullException(nameof(creador));

            Id = _nextId++;
            Name = nombre.Trim();
            Description = descripcion.Trim();
            MaxCapacity = cupoMaximo;
            StartDate = fechaHoraInicio;
            Duration = duracionMinutos;
            Image = imagenPortada;
            Creator = creador;

            Link = $"clase-{Id}-{Guid.NewGuid().ToString().Substring(0, 6)}";

            _registered = new List<User>();
        }

        public void Modificar(string nuevoNombre, string nuevaDescripcion, int nuevoCupoMaximo, DateTimeOffset nuevaFechaHora, int nuevaDuracion, string nuevaImagen = null)
        {
            if (DateTimeOffset.UtcNow >= StartDate) throw new InvalidOperationException("No se puede modificar una clase que ya comenzó");

            if (!string.IsNullOrWhiteSpace(nuevoNombre)) Name = nuevoNombre.Trim();
            if (!string.IsNullOrWhiteSpace(nuevaDescripcion)) Description = nuevaDescripcion.Trim();
            if (nuevoCupoMaximo > 0) MaxCapacity = nuevoCupoMaximo;
            if (nuevaDuracion > 0) Duration = nuevaDuracion;
            StartDate = nuevaFechaHora;
            Image = nuevaImagen;
        }

        public void Eliminar()
        {
            if (_registered.Any()) throw new InvalidOperationException("No se puede eliminar una clase con inscriptos");
            if (DateTimeOffset.UtcNow >= StartDate) throw new InvalidOperationException("No se puede eliminar una clase que ya comenzó");
        }

        public void Inscribir(User usuario)
        {
            if (usuario == null) throw new ArgumentNullException(nameof(usuario));
            if (_registered.Contains(usuario)) throw new InvalidOperationException("Usuario ya inscripto");
            if (_registered.Count >= MaxCapacity) throw new InvalidOperationException("Cupo máximo alcanzado");

            _registered.Add(usuario);
        }

        public void CancelarInscripcion(User usuario)
        {
            if (usuario == null) throw new ArgumentNullException(nameof(usuario));
            if (!_registered.Contains(usuario)) throw new InvalidOperationException("El usuario no está inscripto en la clase");

            var tiempoRestante = StartDate - DateTimeOffset.UtcNow;
            if (tiempoRestante.TotalMinutes < 2) throw new InvalidOperationException("No se puede cancelar la inscripción con menos de 2 minutos de antelación");

            _registered.Remove(usuario);
        }

        public int CuposDisponibles()
        {
            return MaxCapacity - _registered.Count;
        }

        public override string ToString()
        {
            return $"ClaseOnline{{Id={Id}, Nombre={Name}, Inicio={StartDate}, Duracion={Duration}m, Cupos={_registered.Count}/{MaxCapacity}}}";
        }
    }
}
