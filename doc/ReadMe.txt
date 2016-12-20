After regeneration of help documentation replace script in Index.html with this one:

        window.location.replace"html/html/f417d4e0-8296-47cb-989e-bbc32fca222a.htm");

with this one:

        var base = window.location.href;
        base = base.substr(0, base.lastIndexOf("/") + 1);
        window.location.replace(base + "html/f417d4e0-8296-47cb-989e-bbc32fca222a.htm");
