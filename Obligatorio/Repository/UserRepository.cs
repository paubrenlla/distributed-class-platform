using System;
using System.Collections.Generic;
using System.Linq;
using Domain;

namespace Repository
{
    public class UserRepository
    {
        private readonly List<User> _usuarios = new List<User>();

        public User Add(User usuario)
        {
            if (usuario == null) throw new ArgumentNullException(nameof(usuario));
            if (_usuarios.Any(u => u.Username == usuario.Username))
                throw new InvalidOperationException("Ya existe un usuario con ese username");

            _usuarios.Add(usuario);
            return usuario;
        }

        public User GetById(int id)
        {
            return _usuarios.FirstOrDefault(u => u.Id == id);
        }

        public User GetByUsername(string username)
        {
            return _usuarios.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public List<User> GetAll()
        {
            return new List<User>(_usuarios);
        }

        public void Update(User usuario)
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
