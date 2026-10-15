import { useState, useEffect } from 'react'
import { useParams } from "react-router"
import api from './utils/api'
import Table from 'react-bootstrap/Table';

function Tabla(props) {
  var aux = [];

  return (
    <Table>
      <thead>
        <tr><th>Nombre</th><th>Correo</th></tr>
      </thead>
      <tbody>
        {
          props.usuarios && props.usuarios.map(u => <tr><td>{u.name}</td><td>{u.email}</td></tr>)
        }
      </tbody>
    </Table>
  );
}

function Usuarios() {
  const [count, setCount] = useState(0)
  let params = useParams();
  const [usuarios, setUsuarios] = useState([]);

  useEffect(() => {
    api.get('Users')
      .then(response => setUsuarios(response.data))
      .catch(error => console.log(error))
  }, []);

  return (
    <>
      <Tabla usuarios={usuarios}/>
    </>
  )
}

export default Usuarios
