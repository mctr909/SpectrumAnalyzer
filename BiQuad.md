$$\begin{aligned}
	\text{ケイリー変換によるノルムの拘束とパデ近似}
\end{aligned}$$
$$\begin{aligned}
	e^{{\lambda}x}=\frac{1+\lambda t}{1-\lambda t}\quad\left(\lambda^2\in\{-1,0,1\},t=\tan_{\lambda}{\frac{x}{2}},\tan_{\lambda}{\frac{x}{2}}{\approx}\frac{x}{30}\frac{15120+420\lambda^2x^2+\lambda^4x^4}{1008+112\lambda^2x^2+\lambda^4x^4}\right)
\end{aligned}$$
$$\begin{aligned}
	&\text{バイクアッドフィルタ(BPF)での活用例} \\
	&w = \frac{\ln{2}}{2}\frac{\omega}{\sin{\omega}}\textrm{BandWidth} \\
	&\frac{\omega}{\sin{\omega}} = \omega\frac{1+t^2}{2t} = \frac{\omega}{2}\left(t+\frac{1}{t}\right) \\
	&\therefore w = \frac{\ln{2}}{4}\omega\left(t+\frac{1}{t}\right)\textrm{BandWidth}
\end{aligned}$$
$$\begin{aligned}
	&\alpha = \sin{\omega}\sinh{w} = \frac{2t}{1+t^2}\frac{2h}{1-h^2} = \frac{4th}{\left(1+t^2\right)\left(1-h^2\right)} \\
	&\beta = \cos{\omega}=\frac{1-t^2}{1+t^2}
\end{aligned}$$
$$\begin{aligned}
	a_0 &= \alpha + 1, a_1 = 2\frac{\beta}{a_0}, a_2 = \frac{\alpha - 1}{a_0} \\
	b_0 &= \frac{\alpha}{a_0}, b_1 = 0, b_2 = -b_0 \\
	Y_{n} &= a_1Y_{n-1} + a_2Y_{n-2} + b_0X_{n} + b_1X_{n-1} + b_2X_{n-2}
\end{aligned}$$
