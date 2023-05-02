using AutoMapper;
using WebApi.Entities;
using WebApi.Models.Users;
using WebApi.Models;
using System.Linq;
using System;
using System.Collections.Generic;

namespace WebApi.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserModel>();

        }
    }
}