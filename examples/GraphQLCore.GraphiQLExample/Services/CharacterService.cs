﻿namespace GraphQLCore.GraphiQLExample.Services
{
    using Data;
    using Models;
    using System.Collections.Generic;
    using System.Linq;

    public class CharacterService
    {
        private readonly Characters characters = new Characters();

        public IEnumerable<ICharacter> List(Episode episode)
        {
            return this.GetList().Where(e => e.AppearsIn.Contains(episode));
        }

        public Human GetHumanById(string id)
        {
            return this.GetList().SingleOrDefault(e => e.Id == id) as Human;
        }

        public Droid GetDroidById(string id)
        {
            return this.GetList().SingleOrDefault(e => e.Id == id) as Droid;
        }

        private IEnumerable<ICharacter> GetList()
        {
            return new ICharacter[] {
                characters.Artoo, characters.Han, characters.Leia, characters.Luke,
                characters.Tarkin, characters.Threepio, characters.Vader
            };
        }
    }
}
