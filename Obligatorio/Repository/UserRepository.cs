using System;
using System.Collections.Generic;
using System.Linq;
using Domain;

namespace Repository
{
    public class UserRepository
    {
        private readonly List<Usuario> _usuarios = new List<Usuario>();

        public Usuario Add(Usuario usuario)
        {
            if (usuario == null) throw new ArgumentNullException(nameof(usuario));
            if (_usuarios.Any(u => u.Username == usuario.Username))
                throw new InvalidOperationException("Ya existe un usuario con ese username");

            _usuarios.Add(usuario);
            return usuario;
        }

        public Usuario GetById(int id)
        {
            return _usuarios.FirstOrDefault(u => u.Id == id);
        }

        public Usuario GetByUsername(string username)
        {
            return _usuarios.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public List<Usuario> GetAll()
        {
            return new List<Usuario>(_usuarios);
        }

        public void Update(Usuario usuario)
        {
            var existing = GetById(usuario.Id);
            if (existing == null) throw new InvalidOperationException("Usuario no encontrado");
        }

        public void Delete(int id)
        {
            var usuario = GetById(id);
            if (usuario == null) throw new InvalidOperationException("Usuario no encontrado");

            _usuarios.Remove(usuario);
        }
    }
}
