using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Domain;

namespace Repository
{
    public class UserRepository
    {
        private readonly List<User> _usuarios = new List<User>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public User Add(User usuario)
        {
            if (usuario == null) throw new ArgumentNullException(nameof(usuario));

            _semaphore.Wait();
            try
            {
                if (_usuarios.Any(u => u.Username == usuario.Username))
                    throw new InvalidOperationException("Ya existe un usuario con ese username");

                _usuarios.Add(usuario);
                return usuario;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public User GetById(int id)
        {
            return _usuarios.FirstOrDefault(u => u.Id == id);
        }

        public User GetByUsername(string username)
        {
            _semaphore.Wait();
            try
            {
                return _usuarios.FirstOrDefault(u =>
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public List<User> GetAll()
        {
            _semaphore.Wait();
            try
            {
                return new List<User>(_usuarios);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Update(User usuario)
        {
            _semaphore.Wait();
            try
            {
                var existing = _usuarios.FirstOrDefault(u => u.Id == usuario.Id);
                if (existing == null)
                    throw new InvalidOperationException("Usuario no encontrado");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Delete(int id)
        {
            _semaphore.Wait();
            try
            {
                var usuario = _usuarios.FirstOrDefault(u => u.Id == id);
                if (usuario == null)
                    throw new InvalidOperationException("Usuario no encontrado");

                _usuarios.Remove(usuario);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
