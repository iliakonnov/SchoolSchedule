for i in *.png; do echo splash750x1334.png; convert -background white -flatten splash750x1334.png ./white/splash750x1334.png.gif; convert ./white/splash750x1334.png.gif ./white/splash750x1334.png; optipng -strip all -o7 -zm1-9 ./white/splash750x1334.png -out ./opti/splash750x1334.png; done